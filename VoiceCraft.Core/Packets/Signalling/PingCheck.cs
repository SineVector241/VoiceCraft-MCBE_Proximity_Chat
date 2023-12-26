using System;
using VoiceCraft.Core.Packets.Interfaces;

namespace VoiceCraft.Core.Packets.Signalling
{
    public class PingCheck : IPacketData
    {
        public byte[] GetPacketStream()
        {
            return new byte[0];
        }

        public static SignallingPacket Create()
        {
            return new SignallingPacket()
            {
                PacketType = SignallingPacketTypes.PingCheck,
                PacketData = new PingCheck()
            };
        }
    }
}
