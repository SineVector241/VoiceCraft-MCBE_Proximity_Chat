using VoiceCraft.Core.Packets.Interfaces;

namespace VoiceCraft.Core.Packets.MCComm
{
    public class Deny : IMCCommPacketData
    {
        public string Reason { get; set; } = string.Empty;
    }
}
