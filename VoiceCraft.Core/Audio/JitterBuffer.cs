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
        private DateTime FirstPacketTime { get; set; }
        private bool ResetSequence { get; set; }

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
            {
                FirstPacketTime = DateTime.UtcNow;
                CurrentPacketReadCount = inPacket.Sequence - 1;
            }
            ResetSequence = CurrentPacketReadCount - (long)inPacket.Sequence >= uint.MaxValue / 2;

            //Remove Old Packets
            for (int i = 0; i < MaxBufferSize; i++)
            {
                if (BufferedPackets[i].Data != null && BufferedPackets[i].Sequence <= CurrentPacketReadCount && !ResetSequence)
                {
                    BufferedPackets[i].Data = null;
                }
            }

            //Only insert the packet if its not later than the reader.
            if (inPacket.Sequence > CurrentPacketReadCount || ResetSequence)
            {
                //Find an empty slot and insert it.
                for (int i = 0; i < MaxBufferSize; i++)
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
                for (int i = 1; i < MaxBufferSize; i++)
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
        public long Get(ref JitterBufferPacket outPacket)
        {
            //Buffer ain't filled yet.
            if (DateTime.UtcNow.Subtract(FirstPacketTime).TotalMilliseconds < JitterDelay)
            {
                return -1;
            }

            //Find the earliest inserted packet.
            uint earliest = BufferedPackets[0].Sequence;
            int index = 0;
            for (int i = 1; i < MaxBufferSize; i++)
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

            var lost = ResetSequence ? uint.MaxValue - (long)CurrentPacketReadCount + (earliest - 1) : earliest - (long)CurrentPacketReadCount - 1; //We want to get the packets lost. not the difference.
            if (lost > 0 && DateTime.UtcNow.Subtract(BufferedPackets[index].Timestamp).TotalMilliseconds >= JitterDelay) //If its not the next sequence and the inserted packet has exceeded the jitter delay then we return it.
            {
                //Fill the packet and return the amount lost between the last and current sequences.
                outPacket.Length = BufferedPackets[index].Length;
                outPacket.Data = BufferedPackets[index].Data;
                BufferedPackets[index].Data = null;

                CurrentPacketReadCount = earliest;
                return lost;
            }

            //Else we just return the packet if its in the next sequence.
            //Fill the packet and return the amount lost between the last and current sequences.
            outPacket.Length = BufferedPackets[index].Length;
            outPacket.Data = BufferedPackets[index].Data;
            BufferedPackets[index].Data = null;

            CurrentPacketReadCount = earliest;
            return lost;
        }
    }

    public class VoiceCraftJitterBuffer : IWaveProvider, IDisposable
    {
        public int DecodeBufferSize { get; }
        public WaveFormat WaveFormat { get; }
        private JitterBuffer Buffer { get; }
        private OpusDecoder Decoder { get; }
        private System.Timers.Timer DecodeTimer { get; }
        private bool IsDisposed;

        private JitterBufferPacket outPacket = new JitterBufferPacket();
        private JitterBufferPacket inPacket = new JitterBufferPacket();

        private BufferedWaveProvider DecodedBuffer;

        public VoiceCraftJitterBuffer(OpusDecoder decoder, WaveFormat format, int decodeRecordLengthMS)
        {
            Decoder = decoder;
            Buffer = new JitterBuffer(20);
            WaveFormat = format;
            DecodedBuffer = new BufferedWaveProvider(WaveFormat) { DiscardOnBufferOverflow = true, ReadFully = true, BufferDuration = TimeSpan.FromSeconds(2)};

            DecodeBufferSize = decodeRecordLengthMS * WaveFormat.AverageBytesPerSecond / 1000;
            if (DecodeBufferSize % WaveFormat.BlockAlign != 0)
            {
                DecodeBufferSize -= DecodeBufferSize % WaveFormat.BlockAlign;
            }

            DecodeTimer = new System.Timers.Timer(1); //1ms
            DecodeTimer.Elapsed += DecodeNextPacket;
            DecodeTimer.Start();
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            return DecodedBuffer.Read(buffer, offset, count);
        }

        public void Put(byte[] data, uint sequence)
        {
            inPacket.Data = data;
            inPacket.Sequence = sequence;
            inPacket.Length = data.Length;
            inPacket.Timestamp = DateTime.UtcNow;

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

        private void DecodeNextPacket(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                short[] decoded = new short[DecodeBufferSize / 2];
                long lost = Buffer.Get(ref outPacket);

                if (lost == -1) //Empty Packet or Buffer is empty.
                {
                    return;
                }
                else if (lost == 0)
                {
                    decoded = new short[DecodeBufferSize / 2];
                    int shortsRead = Decoder.Decode(outPacket.Data, 0, outPacket.Length, decoded, 0, decoded.Length, false);
                    var decBytes = ShortsToBytes(decoded, 0, shortsRead);
                    DecodedBuffer.AddSamples(decBytes, 0, decBytes.Length);
                }
                else //AHHH MISSING PACKETS! We have to decode twice.
                {
                    //FEC ON
                    int shortsRead = Decoder.Decode(outPacket.Data, 0, outPacket.Length, decoded, 0, decoded.Length, true);
                    var decBytes = ShortsToBytes(decoded, 0, shortsRead);
                    DecodedBuffer.AddSamples(decBytes, 0, decBytes.Length);

                    //FEC OFF
                    shortsRead = Decoder.Decode(outPacket.Data, 0, outPacket.Length, decoded, 0, decoded.Length, true);
                    decBytes = ShortsToBytes(decoded, 0, shortsRead);
                    DecodedBuffer.AddSamples(decBytes, 0, decBytes.Length);
                }
            }
            catch { }
        }

        ~VoiceCraftJitterBuffer()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    DecodeTimer.Stop();
                    DecodeTimer.Dispose();
                    IsDisposed = true;
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }

    public struct JitterBufferPacket
    {
        public uint Sequence;
        public int Length;
        public byte[]? Data;
        public DateTime Timestamp;
    }
}
