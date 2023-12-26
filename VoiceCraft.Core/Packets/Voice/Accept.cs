using VoiceCraft.Core.Packets.Interfaces;

namespace VoiceCraft.Core.Packets.Voice
{
    public class Accept : IPacketData
    {
        public byte[] GetPacketStream()
        {
            return new byte[0];
        }

        public static VoicePacket Create()
        {
            return new VoicePacket()
            {
                PacketType = VoicePacketTypes.Accept,
                PacketData = new Accept()
            };
        }
    }
}
