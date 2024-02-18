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

            //Remove Old Packets
            for (int i = 0; i < MaxBufferSize; i++)
            {
                if (BufferedPackets[i].Data != null && BufferedPackets[i].Sequence <= CurrentPacketReadCount)
                {
                    BufferedPackets[i].Data = null;
                }
            }

            //Only insert the packet if its not later than the reader.
            if (inPacket.Sequence > CurrentPacketReadCount)
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

            var lost = earliest - CurrentPacketReadCount;
            if (lost > 0) lost -= 1; //Get the amount lost.


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

    public struct JitterBufferPacket
    {
        public uint Sequence;
        public int Length;
        public byte[]? Data;
        public DateTime Timestamp;
    }
}