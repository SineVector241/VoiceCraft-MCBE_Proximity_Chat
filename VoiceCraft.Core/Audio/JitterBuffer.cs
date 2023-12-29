using Concentus.Structs;
using NAudio.Utils;
using NAudio.Wave;
using System;
using System.Diagnostics;
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

        public JitterBuffer(int maxBufferSize = 50, int jitterDelayMS = 80)
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

    public class VoiceCraftJitterBuffer : IWaveProvider
    {
        private JitterBuffer Buffer { get; }
        private OpusDecoder Decoder { get; }
        private int DecodeBufferSize { get; }
        public WaveFormat WaveFormat { get; }

        private JitterBufferPacket outPacket = new JitterBufferPacket();
        private JitterBufferPacket inPacket = new JitterBufferPacket();

        private CircularBuffer DecodedBuffer;

        public VoiceCraftJitterBuffer(OpusDecoder decoder, WaveFormat format, int decodeRecordLengthMS)
        {
            Decoder = decoder;
            Buffer = new JitterBuffer();
            WaveFormat = format;

            DecodeBufferSize = decodeRecordLengthMS * WaveFormat.AverageBytesPerSecond / 1000;
            if (DecodeBufferSize % WaveFormat.BlockAlign != 0)
            {
                DecodeBufferSize -= DecodeBufferSize % WaveFormat.BlockAlign;
            }
            DecodedBuffer = new CircularBuffer(DecodeBufferSize * 2); //Should hold onto at least double.
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            var st = new Stopwatch();
            st.Start();
            try
            {
                int bytesRead = 0;
                if (DecodedBuffer.Count != 0)
                {
                    bytesRead += DecodedBuffer.Read(buffer, offset, count);
                    if (DecodedBuffer.Count == 0) return bytesRead; //Return if we have fully filled the required amount.
                }

                var lost = Buffer.Get(ref outPacket);
                if (lost == -1 && bytesRead < count) //Empty Packet or Buffer is empty.
                {
                    Array.Clear(buffer, offset + bytesRead, count - bytesRead);
                    bytesRead = count;
                    return count;
                }
                else if (lost == 0)
                {
                    short[] decoded = new short[DecodeBufferSize / 2];
                    int shortsRead = Decoder.Decode(outPacket.Data, 0, outPacket.Length, decoded, 0, decoded.Length, false);
                    byte[] decodedBytes = ShortsToBytes(decoded, offset / 2, shortsRead); //offset should be able to be divided by 2. If not, IDFK WHY!
                    DecodedBuffer.Write(decodedBytes, 0, decodedBytes.Length); //We use decodedBytes.Length because the count is converted for us in the function.
                }
                else //AHHH MISSING PACKETS! We have to decode twice.
                {
                    short[] decoded = new short[DecodeBufferSize / 2];
                    //FEC ON
                    int shortsRead = Decoder.Decode(outPacket.Data, 0, outPacket.Length, decoded, 0, decoded.Length, true);
                    byte[] decodedBytes = ShortsToBytes(decoded, offset / 2, shortsRead); //offset should be able to be divided by 2. If not, IDFK WHY!
                    DecodedBuffer.Write(decodedBytes, 0, decodedBytes.Length); //Write into buffer

                    //FEC OFF
                    shortsRead = Decoder.Decode(outPacket.Data, 0, outPacket.Length, decoded, 0, decoded.Length, true);
                    decodedBytes = ShortsToBytes(decoded, offset / 2, shortsRead);
                    DecodedBuffer.Write(decodedBytes, 0, decodedBytes.Length); //Write into buffer
                }

                bytesRead += DecodedBuffer.Read(buffer, offset + bytesRead, count - bytesRead);

                if (bytesRead < count) //Yup.
                {
                    Array.Clear(buffer, offset + bytesRead, count - bytesRead);
                    bytesRead = count;
                    st.Stop();
                    return count;
                }
                return bytesRead;
            }
            catch(Exception ex)
            {
                Debug.WriteLine(ex);
                return count;
            }
        }

        public void AddSamples(byte[] data, uint sequence)
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
