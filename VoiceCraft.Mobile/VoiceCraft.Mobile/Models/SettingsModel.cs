namespace VoiceCraft.Mobile.Models
{
    public class SettingsModel
    {
        public int InputDevice { get; set; } = 0;
        public int OutputDevice { get; set; } = 0;
        public ushort WebsocketPort { get; set; } = 8080;
        public float SoftLimiterGain { get; set; } = 5.0f;
        public ushort PreferredPermanentKey { get; set; } = 0;
        public bool DirectionalAudioEnabled { get; set; }
        public bool PinpointPlayerAccuracyEnabled { get; set; }
        public bool ClientSidedPositioning { get; set; }
        public bool PreferredPermanentKeyEnabled { get; set; }
        public bool LinearVolume { get; set; }
        public bool SoftLimiterEnabled { get; set; } = true;
    }
}
