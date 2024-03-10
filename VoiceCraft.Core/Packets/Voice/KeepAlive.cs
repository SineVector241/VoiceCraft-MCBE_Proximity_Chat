using System;
using System.Collections.Generic;
using VoiceCraft.Core.Packets.Interfaces;

namespace VoiceCraft.Core.Packets.Voice
{
    public class KeepAlive : IPacketData
    {
        public int PrivateId { get; set; } = 0;

        public KeepAlive()
        {
            PrivateId = 0;
        }

        public KeepAlive(byte[] dataStream, int readOffset = 0)
        {
            PrivateId = BitConverter.ToInt32(dataStream, readOffset); //Read private Id - 4 bytes.
        }

        public byte[] GetPacketStream()
        {
            var dataStream = new List<byte>();

            dataStream.AddRange(BitConverter.GetBytes(PrivateId));

            return dataStream.ToArray();
        }

        public static VoicePacket Create(int privateId)
        {
            return new VoicePacket()
            {
                PacketType = VoicePacketTypes.KeepAlive,
                PacketData = new KeepAlive()
                {
                    PrivateId = privateId
                }
            };
        }
    }
}
