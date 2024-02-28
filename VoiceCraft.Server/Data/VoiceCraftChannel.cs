using VoiceCraft.Core;

namespace VoiceCraft.Data.Server
{
    public class VoiceCraftChannel : Channel
    {
        public VoiceCraftChannel(string name) : base(name)
        {
        }

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
