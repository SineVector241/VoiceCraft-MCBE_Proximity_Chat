using System;
using System.Collections.Generic;
using VoiceCraft.Network.Packets.Interfaces;

namespace VoiceCraft.Network.Packets.Signalling
{
    public class Undeafen : IPacketData
    {
        public ushort LoginKey { get; set; }

        public Undeafen()
        {
            LoginKey = 0;
        }

        public Undeafen(byte[] dataStream, int readOffset = 0)
        {
            LoginKey = BitConverter.ToUInt16(dataStream, readOffset); //Read login key - 2 bytes.
        }

        public byte[] GetPacketStream()
        {
            var dataStream = new List<byte>();

            dataStream.AddRange(BitConverter.GetBytes(LoginKey));

            return dataStream.ToArray();
        }
    }
}
