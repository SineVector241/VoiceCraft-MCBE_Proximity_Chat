using VoiceCraft.Core.Packets.Interfaces;

namespace VoiceCraft.Core.Packets.MCComm
{
    public class RemoveParticipant : IMCCommPacketData
    {
        public string PlayerId { get; set; } = string.Empty;
    }
}
