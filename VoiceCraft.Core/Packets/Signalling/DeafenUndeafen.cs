using System;
using System.Collections.Generic;
using VoiceCraft.Core.Packets.Interfaces;

namespace VoiceCraft.Core.Packets.Signalling
{
    public class DeafenUndeafen : IPacketData
    {
        public int PrivateId { get; set; }
        public ushort PublicId { get; set; }
        public bool Value { get; set; }

        public DeafenUndeafen()
        {
            PrivateId = 0;
            PublicId = 0;
            Value = false;
        }

        public DeafenUndeafen(byte[] dataStream, int readOffset = 0)
        {
            PrivateId = BitConverter.ToInt32(dataStream, readOffset); //Read login Id - 4 bytes.
            PublicId = BitConverter.ToUInt16(dataStream, readOffset + 4); //Read login key - 2 bytes.
            Value = BitConverter.ToBoolean(dataStream, readOffset + 6); //Read value - 1 byte.
        }

        public byte[] GetPacketStream()
        {
            var dataStream = new List<byte>();

            dataStream.AddRange(BitConverter.GetBytes(PrivateId));
            dataStream.AddRange(BitConverter.GetBytes(PublicId));
            dataStream.AddRange(BitConverter.GetBytes(Value));

            return dataStream.ToArray();
        }

        public static SignallingPacket Create(int privateId, ushort publicId, bool value)
        {
            return new SignallingPacket()
            {
                PacketType = SignallingPacketTypes.DeafenUndeafen,
                PacketData = new DeafenUndeafen()
                {
                    PrivateId = privateId,
                    PublicId = publicId,
                    Value = value
                }
            };
        }
    }
}
