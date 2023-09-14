using VoiceCraft.Network.Packets.Interfaces;

namespace VoiceCraft.Network.Packets.Signalling
{
    public class Null : IPacketData
    {
        public byte[] GetPacketStream()
        {
            return new byte[0];
        }
    }
}
