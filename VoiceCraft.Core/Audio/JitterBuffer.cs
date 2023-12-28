using Concentus.Structs;
using NAudio.Wave;
using System;

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
                LatestPacketInserted = DateTime.UtcNow;
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
            for (i = 0; i < MaxBufferSize; i++)
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


        public VoiceCraftJitterBuffer(OpusDecoder decoder, int decodeBufferSize, WaveFormat format)
        {
            Decoder = decoder;
            Buffer = new JitterBuffer(100, 80);
            WaveFormat = format;
            DecodeBufferSize = decodeBufferSize;
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            throw new System.NotImplementedException();
        }
    }

    public struct JitterBufferPacket
    {
        public uint Sequence;
        public int Length;
        public byte[]? Data;
    }
}
