using System;
using System.Collections.Generic;
using VoiceCraft.Core.Packets.Interfaces;

namespace VoiceCraft.Core.Packets.Signalling
{
    public class Deafen : IPacketData
    {
        public ushort LoginKey { get; set; }

        public Deafen()
        {
            LoginKey = 0;
        }

        public Deafen(byte[] dataStream, int readOffset = 0)
        {
            LoginKey = BitConverter.ToUInt16(dataStream, readOffset); //Read login key - 2 bytes.
        }

        public byte[] GetPacketStream()
        {
            var dataStream = new List<byte>();

            dataStream.AddRange(BitConverter.GetBytes(LoginKey));

            return dataStream.ToArray();
        }

        public static SignallingPacket Create(ushort loginKey)
        {
            return new SignallingPacket()
            {
                PacketType = SignallingPacketTypes.Deafen,
                PacketData = new Deafen()
                {
                    LoginKey = loginKey
                }
            };
        }
    }
}
