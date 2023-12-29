using Concentus.Structs;
using NAudio.Wave;
using System;
using System.Linq;

namespace VoiceCraft.Core.Audio
{
    public class JitterBuffer
    {
        public int MaxBufferSize { get; set; }
        public int JitterDelay { get; set; }
        public uint CurrentPacketReadCount { get; set; }

        private JitterBufferPacket[] BufferedPackets { get; set; }
        private long FirstPacketTick { get; set; }

        public JitterBuffer(int maxBufferSize = 50, int jitterDelayMS = 100)
        {
            MaxBufferSize = maxBufferSize;
            JitterDelay = jitterDelayMS;
            BufferedPackets = new JitterBufferPacket[maxBufferSize];
        }

        /// <summary>
        /// Inputs a packet into the buffer.
        /// </summary>
        /// <param name="inPacket"></param>
        public void Put(JitterBufferPacket inPacket)
        {
            //If the buffer is empty. We know it's the first packet.
            if (BufferedPackets.Count(x => x.Data != null) == 0)
                FirstPacketTick = DateTime.UtcNow.Ticks;

            //Remove Old Packets
            for (int i = 0; i < MaxBufferSize; i++)
            {
                if (BufferedPackets[i].Data != null && BufferedPackets[i].Sequence <= CurrentPacketReadCount)
                {
                    BufferedPackets[i].Data = null;
                }
            }

            //Only insert the packet if its not later than the reader.
            if(inPacket.Sequence > CurrentPacketReadCount)
            {
                //Find an empty slot and insert it.
                for(int i = 0; i < MaxBufferSize; i++)
                {
                    if (BufferedPackets[i].Data == null)
                    {
                        BufferedPackets[i].Sequence = inPacket.Sequence;
                        BufferedPackets[i].Data = inPacket.Data;
                        BufferedPackets[i].Length = inPacket.Length;
                        return;
                    }
                }

                //We haven't found an empty slot so we discard the oldest/highest packet sequence.
                uint oldest = BufferedPackets[0].Sequence;
                int index = 0;
                for(int i = 1; i < MaxBufferSize; i++)
                {
                    if (BufferedPackets[i].Sequence > oldest)
                    {
                        oldest = BufferedPackets[i].Sequence;
                        index = i;
                    }
                }

                //Replace the old packet with the new packet.
                BufferedPackets[index].Data = inPacket.Data;
                BufferedPackets[index].Sequence = inPacket.Sequence;
                BufferedPackets[index].Length = inPacket.Length;
            }
        }

        /// <summary>
        /// Gets a packet and removes it from the buffer.
        /// </summary>
        /// <param name="outPacket"></param>
        /// <returns>The number of lost packets between the last and current Get calls.</returns>
        public int Get(ref JitterBufferPacket outPacket)
        {
            //Buffer ain't filled yet.
            if((DateTime.UtcNow.Ticks - FirstPacketTick) < JitterDelay)
            {
                return -1;
            }

            //Find the earliest inserted packet.
            uint earliest = BufferedPackets[0].Sequence;
            int index = 0;
            for(int i = 1; i < MaxBufferSize; i++)
            {
                if (BufferedPackets[i].Data != null && BufferedPackets[i].Sequence < earliest)
                {
                    earliest = BufferedPackets[i].Sequence;
                    index = i;
                }
            }

            if (BufferedPackets[index].Data == null) //We can assume the buffer is empty so we return -1;
            {
                return -1;
            }

            //Else we can fill the packet and return the amount lost between the last and current sequences.
            outPacket.Length = BufferedPackets[index].Length;
            outPacket.Data = BufferedPackets[index].Data;
            BufferedPackets[index].Data = null;
            var lost = (int)(earliest - CurrentPacketReadCount - 1); //We want to get the packets lost. not the difference.
            CurrentPacketReadCount = earliest;

            return lost;
        }
    }

    public class VoiceCraftJitterBuffer
    {
        public int DecodeBufferSize { get; }
        public WaveFormat WaveFormat { get; }
        private JitterBuffer Buffer { get; }
        private OpusDecoder Decoder { get; }

        private JitterBufferPacket outPacket = new JitterBufferPacket();
        private JitterBufferPacket inPacket = new JitterBufferPacket();

        private byte[]? NextDecodedPacket;

        public VoiceCraftJitterBuffer(OpusDecoder decoder, WaveFormat format, int decodeRecordLengthMS)
        {
            Decoder = decoder;
            Buffer = new JitterBuffer(20);
            WaveFormat = format;

            DecodeBufferSize = decodeRecordLengthMS * WaveFormat.AverageBytesPerSecond / 1000;
            if (DecodeBufferSize % WaveFormat.BlockAlign != 0)
            {
                DecodeBufferSize -= DecodeBufferSize % WaveFormat.BlockAlign;
            }
        }

        public int Get(byte[] decodedBytes)
        {
            if(decodedBytes.Length != DecodeBufferSize)
                throw new ArgumentException(nameof(decodedBytes), "Must be the same as the DecodeBufferSize!");

            if(NextDecodedPacket != null)
            {
                decodedBytes = NextDecodedPacket;
                NextDecodedPacket = null;
                return decodedBytes.Length;
            }

            short[] decoded = new short[DecodeBufferSize / 2];
            var lost = Buffer.Get(ref outPacket);

            try
            {
                if (lost == -1) //Empty Packet or Buffer is empty.
                {
                    return -1;
                }
                else if (lost == 0)
                {
                    decoded = new short[DecodeBufferSize / 2];
                    int shortsRead = Decoder.Decode(outPacket.Data, 0, outPacket.Length, decoded, 0, decoded.Length, false);
                    var decBytes = ShortsToBytes(decoded, 0, shortsRead);
                    System.Buffer.BlockCopy(decBytes, 0, decodedBytes, 0, decBytes.Length); //WAY FASTER TO USE THIS THAN ARRAY.COPY();
                    return decodedBytes.Length;
                }
                else //AHHH MISSING PACKETS! We have to decode twice.
                {
                    //FEC ON
                    int shortsRead = Decoder.Decode(outPacket.Data, 0, outPacket.Length, decoded, 0, decoded.Length, true);
                    NextDecodedPacket = ShortsToBytes(decoded, 0, shortsRead);

                    //FEC OFF
                    shortsRead = Decoder.Decode(outPacket.Data, 0, outPacket.Length, decoded, 0, decoded.Length, true);
                    var decBytes = ShortsToBytes(decoded, 0, shortsRead);
                    System.Buffer.BlockCopy(decBytes, 0, decodedBytes, 0, decBytes.Length); //WAY FASTER TO USE THIS THAN ARRAY.COPY();
                    return decodedBytes.Length;
                }
            }
            catch
            {
                return -1;
            }
        }

        public void Put(byte[] data, uint sequence)
        {
            inPacket.Data = data;
            inPacket.Sequence = sequence;
            inPacket.Length = data.Length;

            Buffer.Put(inPacket);
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
