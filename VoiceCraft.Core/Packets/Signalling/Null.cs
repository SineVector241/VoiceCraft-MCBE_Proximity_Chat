using VoiceCraft.Core.Packets.Interfaces;

namespace VoiceCraft.Core.Packets.Signalling
{
    public class Null : IPacketData
    {
        public byte[] GetPacketStream()
        {
            return new byte[0];
        }

        public static SignallingPacket Create(SignallingPacketTypes packetType)
        {
            return new SignallingPacket()
            {
                PacketType = packetType,
                PacketData = new Null()
            };
        }
    }
}
