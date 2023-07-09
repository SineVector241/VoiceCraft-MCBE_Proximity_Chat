namespace VoiceCraft.Windows.Models
{
    public class SettingsModel
    {
        public int InputDevice { get; set; } = 0;
        public int OutputDevice { get; set; } = 0;
        public int WebsocketPort { get; set; } = 8080;
        public float SoftLimiterGain { get; set; } = 5.0f;
        public bool DirectionalAudioEnabled { get; set; }
        public bool PinpointPlayerAccuracyEnabled { get; set; }
        public bool ClientSidedPositioning { get; set; }
        public bool SoftLimiterEnabled { get; set; } = true;
    }
}
