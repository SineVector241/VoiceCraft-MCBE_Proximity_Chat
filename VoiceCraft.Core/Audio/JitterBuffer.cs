using Concentus.Structs;
using NAudio.Wave;
using System;
using System.Linq;

namespace VoiceCraft.Core.Audio
{
    public class JitterBuffer
    {
        public int MaxBufferSize { get; }
        JitterBufferPacket[] BufferedPackets { get; }

        private uint CurrentReadSequenceNumber;
        private DateTime LatestPacketInserted;
        private int BufferedDelay;

        public JitterBuffer(int maxBufferSize, int bufferDelayMS)
        {
            if(bufferDelayMS <= 0)
                throw new ArgumentOutOfRangeException(nameof(bufferDelayMS), "Cannot be lower than 1!");

            MaxBufferSize = maxBufferSize;
            BufferedPackets = new JitterBufferPacket[MaxBufferSize];
            BufferedDelay = bufferDelayMS;
        }

        public void PutPacket(JitterBufferPacket packet)
        {
            if(BufferedPackets.Count(x => x.Data != null) == 0)
                LatestPacketInserted = DateTime.UtcNow;

            int i, j;

            //Cleanup Old Packets
            for (i = 0; i < MaxBufferSize; i++)
            {
                if (BufferedPackets[i].Data != null && BufferedPackets[i].Sequence <= CurrentReadSequenceNumber)
                {
                    BufferedPackets[i].Data = null;
                }
            }

            if (packet.Sequence > CurrentReadSequenceNumber)
            {
                //Find an empty slot.
                for (i = 0; i < MaxBufferSize; i++)
                {
                    if (BufferedPackets[i].Data == null)
                        break;
                }

                //No room so we discard the earliest packet and insert.
                if (i == MaxBufferSize)
                {
                    var earliest = BufferedPackets[0].Sequence;
                    i = 0;
                    for (j = 1; j < MaxBufferSize; j++)
                    {
                        if (BufferedPackets[i].Data == null || BufferedPackets[j].Sequence < earliest)
                        {
                            earliest = BufferedPackets[j].Sequence;
                            i = j;
                        }
                    }
                }

                BufferedPackets[i].Data = packet.Data;
                BufferedPackets[i].Sequence = packet.Sequence;
                BufferedPackets[i].Length = packet.Length;
            }
        }

        /// <summary>
        /// Gets a jitter buffered packet.
        /// </summary>
        /// <param name="packet"></param>
        /// <returns>Number of packets missing between the returned packet and the last returned packet. If no packet is retrieved then -1 is returned.</returns>
        public int Get(ref JitterBufferPacket packet)
        {
            if(DateTime.UtcNow.Subtract(LatestPacketInserted).TotalMilliseconds < BufferedDelay) //Don't want to instantly return packets as they were inserted.
            {
                return -1;
            }

            int i;
            uint oldest = 0;
            var found = false;
            var lost = 0;
            for (i = 0; i < MaxBufferSize - 1; i++)
            {
                if (BufferedPackets[i].Data != null && (!found || BufferedPackets[i].Sequence < oldest))
                {
                    oldest = BufferedPackets[i].Sequence;
                    found = true;
                }
            }

            if (found)
            {
                lost = (int)(oldest - CurrentReadSequenceNumber) - 1;
                CurrentReadSequenceNumber = oldest;
            }
            else
            {
                return -1;
            }

            packet.Data = BufferedPackets[i].Data;
            BufferedPackets[i].Data = null; //Remove the packet
            packet.Sequence = BufferedPackets[i].Sequence;
            packet.Length = BufferedPackets[i].Length;

            return lost; //Return number of lost packets.
        }
    }

    public class VoiceCraftJitterBuffer : IWaveProvider
    {
        private JitterBuffer Buffer { get; }
        private OpusDecoder Decoder { get; }
        private int DecodeBufferSize { get; }
        public WaveFormat WaveFormat { get; }

        private JitterBufferPacket outPacket = new JitterBufferPacket();
        private JitterBufferPacket inPacket = new JitterBufferPacket();
        private byte[] LeftoverBuffer;


        public VoiceCraftJitterBuffer(OpusDecoder decoder, WaveFormat format, int decodeRecordLengthMS)
        {
            Decoder = decoder;
            Buffer = new JitterBuffer(100, 80);
            WaveFormat = format;

            DecodeBufferSize = decodeRecordLengthMS * WaveFormat.AverageBytesPerSecond / 1000;
            if (DecodeBufferSize % WaveFormat.BlockAlign != 0)
            {
                DecodeBufferSize -= DecodeBufferSize % WaveFormat.BlockAlign;
            }
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            return count;
        }

        public void AddSamples(byte[] data, uint sequence)
        {
            inPacket.Data = data;
            inPacket.Sequence = sequence;
            inPacket.Length = data.Length;

            Buffer.PutPacket(inPacket);
        }

        private static byte[] ShortsToBytes(short[] input, int offset, int length)
        {
            byte[] processedValues = new byte[length * 2];
            for (int c = 0; c < length; c++)
            {
                processedValues[c * 2] = (byte)(input[c + offset] & 0xFF);
                processedValues[c * 2 + 1] = (byte)(input[c + offset] >> 8 & 0xFF);
            }

            return processedValues;
        }
    }

    public struct JitterBufferPacket
    {
        public uint Sequence;
        public int Length;
        public byte[]? Data;
    }
}
