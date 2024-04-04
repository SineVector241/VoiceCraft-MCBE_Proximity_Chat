using System.Collections.Generic;
using VoiceCraft.Core.Packets;

namespace VoiceCraft.Core
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
        public List<MCCommPacketId> InboundMCCommFilter { get; set; } = new List<MCCommPacketId>();
        public List<MCCommPacketId> OutboundMCCommFilter { get; set; } = new List<MCCommPacketId>();
    }
}