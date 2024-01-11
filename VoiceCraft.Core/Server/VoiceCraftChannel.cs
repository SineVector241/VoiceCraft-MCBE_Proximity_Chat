namespace VoiceCraft.Core.Server
{
    public class VoiceCraftChannel
    {
        public string Name { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public ChannelSettings? OverrideSettings { get; set; }
    }

    public class ChannelSettings
    {
        public int? ProximityDistance { get; set; } = 30;
        public bool? ProximityToggle { get; set; }
        public bool? VoiceEffects { get; set; }
    }
}
