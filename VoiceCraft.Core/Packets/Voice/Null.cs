using VoiceCraft.Core.Packets.Interfaces;

namespace VoiceCraft.Core.Packets.Voice
{
    public class Null : IPacketData
    {
        public byte[] GetPacketStream()
        {
            return new byte[0];
        }

        public static VoicePacket Create(VoicePacketTypes packetType)
        {
            return new VoicePacket()
            {
                PacketType = packetType,
                PacketData = new Null()
            };
        }
    }
}
