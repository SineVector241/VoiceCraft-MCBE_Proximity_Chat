using System;
using System.Collections.Generic;
using VoiceCraft.Core.Packets.Interfaces;

namespace VoiceCraft.Core.Packets.Signalling
{
    public class DeafenUndeafen : IPacketData
    {
        public ushort LoginKey { get; set; }
        public bool Value { get; set; }

        public DeafenUndeafen()
        {
            LoginKey = 0;
            Value = false;
        }

        public DeafenUndeafen(byte[] dataStream, int readOffset = 0)
        {
            LoginKey = BitConverter.ToUInt16(dataStream, readOffset); //Read login key - 2 bytes.
            Value = BitConverter.ToBoolean(dataStream, readOffset + 2); //Read value - 1 byte.
        }

        public byte[] GetPacketStream()
        {
            var dataStream = new List<byte>();

            dataStream.AddRange(BitConverter.GetBytes(LoginKey));
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
                    LoginKey = loginKey,
                    Value = value
                }
            };
        }
    }
}
