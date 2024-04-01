using VoiceCraft.Core.Packets;
using VoiceCraft.Network.Packets;

namespace VoiceCraft.Network
{
    public class DebugProperties
    {
        public bool LogExceptions { get; set; } = false;
        public bool LogInboundPackets { get; set; } = false;
        public bool LogOutboundPackets { get; set; } = false;
        public bool LogInboundMCCommPackets { get; set; } = false;
        public bool LogOutboundMCCommPackets { get; set; } = false;
        public List<VoiceCraftPacketTypes> InboundPacketFilter { get; set; } = new List<VoiceCraftPacketTypes>();
        public List<VoiceCraftPacketTypes> OutboundPacketFilter { get; set; } = new List<VoiceCraftPacketTypes>();
        public List<MCCommPacketTypes> InboundMCCommFilter { get; set; } = new List<MCCommPacketTypes>();
        public List<MCCommPacketTypes> OutboundMCCommFilter { get; set; } = new List<MCCommPacketTypes>();
    }
}