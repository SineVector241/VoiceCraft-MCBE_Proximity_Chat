using VoiceCraft.Core.Packets.Interfaces;

namespace VoiceCraft.Core.Packets.MCComm
{
    public class Login : IMCCommPacketData
    {
        public string LoginKey { get; set; } = string.Empty;
    }
}
