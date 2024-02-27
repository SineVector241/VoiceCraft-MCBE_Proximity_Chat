using System;
using System.Collections.Generic;
using VoiceCraft.Core.Packets.Interfaces;

namespace VoiceCraft.Core.Packets.Signalling
{
    public class Logout : IPacketData
    {
        public ushort Key { get; set; }

        public Logout()
        {
            Key = 0;
        }

        public Logout(byte[] dataStream, int readOffset = 0)
        {
            Key = BitConverter.ToUInt16(dataStream, readOffset); //Read login key - 2 bytes.
        }

        public byte[] GetPacketStream()
        {
            var dataStream = new List<byte>();

            dataStream.AddRange(BitConverter.GetBytes(Key));

            return dataStream.ToArray();
        }

        public static SignallingPacket Create(ushort loginKey)
        {
            return new SignallingPacket()
            {
                PacketType = SignallingPacketTypes.Logout,
                PacketData = new Logout()
                {
                    Key = loginKey
                }
            };
        }
    }
}
