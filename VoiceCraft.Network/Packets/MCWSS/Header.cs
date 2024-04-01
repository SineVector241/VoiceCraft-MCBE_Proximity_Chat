namespace VoiceCraft.Core.Packets.MCWSS
{
    public class Header
    {
        public string requestId { get; set; } = string.Empty;
        public string messagePurpose { get; set; } = string.Empty;
        public int version { get; set; } = 1;
        public string messageType { get; set; } = string.Empty;
        public string eventName { get; set; } = string.Empty;
    }
}
