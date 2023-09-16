using VoiceCraft.Core.Packets.Interfaces;

namespace VoiceCraft.Core.Packets.Voice
{
    public class Login : IPacketData
    {
        public byte[] GetPacketStream()
        {
            return new byte[0];
        }
    }
}
