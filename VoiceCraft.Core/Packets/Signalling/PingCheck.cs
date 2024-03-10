using System;
using System.Collections.Generic;
using VoiceCraft.Core.Packets.Interfaces;

namespace VoiceCraft.Core.Packets.Signalling
{
    public class PingCheck : IPacketData
    {
        public int PrivateId { get; set; } = 0;

        public PingCheck()
        {
            PrivateId = 0;
        }

        public PingCheck(byte[] dataStream, int readOffset = 0)
        {
            PrivateId = BitConverter.ToInt32(dataStream, readOffset); //Read login Id - 4 bytes.
        }

        public byte[] GetPacketStream()
        {
            var dataStream = new List<byte>();

            dataStream.AddRange(BitConverter.GetBytes(PrivateId));

            return dataStream.ToArray();
        }

        public static SignallingPacket Create(int privateId)
        {
            return new SignallingPacket()
            {
                PacketType = SignallingPacketTypes.PingCheck,
                PacketData = new PingCheck()
                {
                    PrivateId = privateId
                }
            };
        }
    }
}
