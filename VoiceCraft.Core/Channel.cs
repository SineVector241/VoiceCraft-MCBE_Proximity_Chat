namespace VoiceCraft.Core
{
    public class Channel
    {
        public string Name { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public ChannelOverride? OverrideSettings { get; set; }
    }

    public class ChannelOverride
    {
        public int ProximityDistance { get; set; } = 30;
        public bool ProximityToggle { get; set; } = true;
        public bool VoiceEffects { get; set; } = true;
    }
}
