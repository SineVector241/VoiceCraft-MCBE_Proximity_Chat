using VoiceCraft.Network.Packets.Interfaces;

namespace VoiceCraft.Network.Packets.Voice
{
    public class Null : IPacketData
    {
        public byte[] GetPacketStream()
        {
            return new byte[0];
        }
    }
}
