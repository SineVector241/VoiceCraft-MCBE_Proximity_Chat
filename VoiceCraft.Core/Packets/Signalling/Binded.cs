using System.Collections.Generic;
using System;
using VoiceCraft.Core.Packets.Interfaces;
using System.Text;

namespace VoiceCraft.Core.Packets.Signalling
{
    public class Binded : IPacketData
    {
        public string Name { get; set; } = string.Empty;

        public Binded()
        {
            Name = string.Empty;
        }

        public Binded(byte[] dataStream, int readOffset = 0)
        {
            var nameLength = BitConverter.ToInt32(dataStream, readOffset); //read name length - 4 bytes.

            if(nameLength > 0)
                Name = Encoding.UTF8.GetString(dataStream, readOffset + 4, nameLength); //read name variable.
        }

        public byte[] GetPacketStream()
        {
            var dataStream = new List<byte>();

            if (!string.IsNullOrWhiteSpace(Name))
                dataStream.AddRange(BitConverter.GetBytes(Name.Length));
            else
                dataStream.AddRange(BitConverter.GetBytes(0));

            if (!string.IsNullOrWhiteSpace(Name))
                dataStream.AddRange(Encoding.UTF8.GetBytes(Name));

            return dataStream.ToArray();
        }

        public static SignallingPacket Create(string name)
        {
            return new SignallingPacket()
            {
                PacketType = SignallingPacketTypes.Binded,
                PacketData = new Binded()
                {
                    Name = name,
                }
            };
        }
    }
}
