using System.Collections.Generic;
using System;
using VoiceCraft.Core.Packets.Interfaces;
using System.Text;

namespace VoiceCraft.Core.Packets.Signalling
{
    public class BindedUnbinded : IPacketData
    {
        public string Name { get; set; } = string.Empty;
        public bool Value { get; set; }

        public BindedUnbinded()
        {
            Name = string.Empty;
            Value = false;
        }

        public BindedUnbinded(byte[] dataStream, int readOffset = 0)
        {
            Value = BitConverter.ToBoolean(dataStream, readOffset); //Read value - 1 byte.
            var nameLength = BitConverter.ToInt32(dataStream, readOffset + 1); //read name length - 4 bytes.

            if(nameLength > 0)
                Name = Encoding.UTF8.GetString(dataStream, readOffset + 5, nameLength); //read name variable.
        }

        public byte[] GetPacketStream()
        {
            var dataStream = new List<byte>();

            dataStream.AddRange(BitConverter.GetBytes(Value));

            if (!string.IsNullOrWhiteSpace(Name))
                dataStream.AddRange(BitConverter.GetBytes(Name.Length));
            else
                dataStream.AddRange(BitConverter.GetBytes(0));

            if (!string.IsNullOrWhiteSpace(Name))
                dataStream.AddRange(Encoding.UTF8.GetBytes(Name));

            return dataStream.ToArray();
        }

        public static SignallingPacket Create(string name, bool value)
        {
            return new SignallingPacket()
            {
                PacketType = SignallingPacketTypes.BindedUnbinded,
                PacketData = new BindedUnbinded()
                {
                    Name = name,
                    Value = value
                }
            };
        }
    }
}
