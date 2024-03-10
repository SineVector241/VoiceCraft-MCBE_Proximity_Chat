using System.Collections.Generic;
using System;
using VoiceCraft.Core.Packets.Interfaces;
using System.Text;

namespace VoiceCraft.Core.Packets.Signalling
{
    public class BindedUnbinded : IPacketData
    {
        public int PrivateId { get; set; } = 0;
        public bool Binded { get; set; }
        public string Name { get; set; } = string.Empty;

        public BindedUnbinded()
        {
            PrivateId = 0;
            Binded = false;
            Name = string.Empty;
        }

        public BindedUnbinded(byte[] dataStream, int readOffset = 0)
        {
            PrivateId = BitConverter.ToInt32(dataStream, readOffset); //Read private Id - 4 bytes.
            Binded = BitConverter.ToBoolean(dataStream, readOffset + 4); //Read value - 1 byte.
            var nameLength = BitConverter.ToInt32(dataStream, readOffset + 5); //read name length - 4 bytes.

            if(nameLength > 0)
                Name = Encoding.UTF8.GetString(dataStream, readOffset + 9, nameLength); //read name variable.
        }

        public byte[] GetPacketStream()
        {
            var dataStream = new List<byte>();

            dataStream.AddRange(BitConverter.GetBytes(PrivateId));
            dataStream.AddRange(BitConverter.GetBytes(Binded));

            if (!string.IsNullOrWhiteSpace(Name))
                dataStream.AddRange(BitConverter.GetBytes(Name.Length));
            else
                dataStream.AddRange(BitConverter.GetBytes(0));

            if (!string.IsNullOrWhiteSpace(Name))
                dataStream.AddRange(Encoding.UTF8.GetBytes(Name));

            return dataStream.ToArray();
        }

        public static SignallingPacket Create(int privateId, string name, bool value)
        {
            return new SignallingPacket()
            {
                PacketType = SignallingPacketTypes.BindedUnbinded,
                PacketData = new BindedUnbinded()
                {
                    Name = name,
                    Binded = value,
                    PrivateId = privateId
                }
            };
        }
    }
}
