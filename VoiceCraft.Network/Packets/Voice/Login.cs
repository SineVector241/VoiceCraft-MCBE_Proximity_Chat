using System;
using System.Collections.Generic;
using System.Text;
using VoiceCraft.Network.Packets.Interfaces;

namespace VoiceCraft.Network.Packets.Voice
{
    public class Login : IPacketData
    {
        public byte[] GetPacketStream()
        {
            return new byte[0];
        }
    }
}
