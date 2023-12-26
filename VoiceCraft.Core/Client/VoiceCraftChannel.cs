namespace VoiceCraft.Core.Client
{
    public class VoiceCraftChannel
    {
        public string Name { get; set; } = string.Empty;
        public bool RequiresPassword { get; set; } = false;
        public byte ChannelId { get; set; }
    }
}
