using System.Collections.Generic;
using VoiceCraft.Core.Packets;

namespace VoiceCraft.Core.Server
{
    public class DebugProperties
    {
        public bool LogExceptions { get; set; } = false;
        public bool LogInboundVoicePackets { get; set; } = false;
        public bool LogOutboundVoicePackets { get; set; } = false;
        public bool LogInboundSignallingPackets { get; set; } = false;
        public bool LogOutboundSignallingPackets { get; set; } = false;
        public bool LogInboundMCCommPackets { get; set; } = false;
        public List<VoicePacketTypes> InboundVoiceFilter { get; set; } = new List<VoicePacketTypes>();
        public List<VoicePacketTypes> OutboundVoiceFilter { get; set; } = new List<VoicePacketTypes>();
        public List<SignallingPacketTypes> InboundSignallingFilter { get; set; } = new List<SignallingPacketTypes>();
        public List<SignallingPacketTypes> OutboundSignallingFilter { get; set; } = new List<SignallingPacketTypes>();
        public List<MCCommPacketTypes> InboundMCCommFilter { get; set; } = new List<MCCommPacketTypes>();
    }
}
