using VoiceCraft.Network.Packets.Interfaces;

namespace VoiceCraft.Network.Packets.Voice
{
    public class Accept : IPacketData
    {
        public byte[] GetPacketStream()
        {
            return new byte[0];
        }
    }
}
