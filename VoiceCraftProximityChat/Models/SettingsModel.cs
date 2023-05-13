namespace VoiceCraftProximityChat.Models
{
    public class SettingsModel
    {
        public int InputDevice { get; set; } = -1;
        public int OutputDevice { get; set; } = -1;
        public bool DirectionalAudioEnabled { get; set; }
        public bool PinpointPlayerAccuracyEnabled { get; set; }
    }
}
