using System;
using System.Collections.Generic;
using VoiceCraft.Network.Packets.Interfaces;

namespace VoiceCraft.Network.Packets.Signalling
{
    public class Unmute : IPacketData
    {
        public ushort LoginKey { get; set; }

        public Unmute()
        {
            LoginKey = 0;
        }

        public Unmute(byte[] dataStream, int readOffset = 0)
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
