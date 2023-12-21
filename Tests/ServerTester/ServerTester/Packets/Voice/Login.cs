using System;
using System.Collections.Generic;
using System.Diagnostics;
using VoiceCraft.Core.Packets.Interfaces;

namespace VoiceCraft.Core.Packets.Voice
{
    public class Login : IPacketData
    {
        public ushort LoginKey { get; set; }

        public Login(byte[] dataStream, int readOffset = 0)
        {
            LoginKey = BitConverter.ToUInt16(dataStream, readOffset); //read login key - 2 bytes.
        }

        public Login()
        {
            LoginKey = 0;
        }

        public byte[] GetPacketStream()
        {
            var dataStream = new List<byte>();

            dataStream.AddRange(BitConverter.GetBytes(LoginKey));

            return dataStream.ToArray();
        }
    }
}
