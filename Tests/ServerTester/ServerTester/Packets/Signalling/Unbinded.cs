using VoiceCraft.Core.Packets.Interfaces;

namespace VoiceCraft.Core.Packets.Signalling
{
    public class Unbinded : IPacketData
    {
        public byte[] GetPacketStream()
        {
            return new byte[0];
        }
    }
}
