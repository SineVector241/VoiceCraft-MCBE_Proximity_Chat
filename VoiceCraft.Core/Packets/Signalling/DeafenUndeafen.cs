using System;
using System.Collections.Generic;
using VoiceCraft.Core.Packets.Interfaces;

namespace VoiceCraft.Core.Packets.Signalling
{
    public class DeafenUndeafen : IPacketData
    {
        public ushort Key { get; set; }
        public bool Value { get; set; }

        public DeafenUndeafen()
        {
            Key = 0;
            Value = false;
        }

        public DeafenUndeafen(byte[] dataStream, int readOffset = 0)
        {
            Key = BitConverter.ToUInt16(dataStream, readOffset); //Read login key - 2 bytes.
            Value = BitConverter.ToBoolean(dataStream, readOffset + 2); //Read value - 1 byte.
        }

        public byte[] GetPacketStream()
        {
            var dataStream = new List<byte>();

            dataStream.AddRange(BitConverter.GetBytes(Key));
            dataStream.AddRange(BitConverter.GetBytes(Value));

            return dataStream.ToArray();
        }

        public static SignallingPacket Create(ushort loginKey, bool value)
        {
            return new SignallingPacket()
            {
                PacketType = SignallingPacketTypes.DeafenUndeafen,
                PacketData = new DeafenUndeafen()
                {
                    Key = loginKey,
                    Value = value
                }
            };
        }
    }
}
