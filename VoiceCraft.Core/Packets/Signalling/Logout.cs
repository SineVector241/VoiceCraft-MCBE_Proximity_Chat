using System;
using System.Collections.Generic;
using VoiceCraft.Core.Packets.Interfaces;

namespace VoiceCraft.Core.Packets.Signalling
{
    public class Logout : IPacketData
    {
        public ushort PublicId { get; set; }
        public int PrivateId { get; set; }

        public Logout()
        {
            PublicId = 0;
            PrivateId = 0;
        }

        public Logout(byte[] dataStream, int readOffset = 0)
        {
            PublicId = BitConverter.ToUInt16(dataStream, readOffset); //Read public id - 2 bytes.
            PrivateId = BitConverter.ToInt32(dataStream, readOffset + 2); //Read private Id - 4 bytes.
        }

        public byte[] GetPacketStream()
        {
            var dataStream = new List<byte>();

            dataStream.AddRange(BitConverter.GetBytes(PublicId));
            dataStream.AddRange(BitConverter.GetBytes(PrivateId));

            return dataStream.ToArray();
        }

        public static SignallingPacket Create(int privateId,ushort publicId)
        {
            return new SignallingPacket()
            {
                PacketType = SignallingPacketTypes.Logout,
                PacketData = new Logout()
                {
                    PublicId = publicId,
                    PrivateId = privateId
                }
            };
        }
    }
}
