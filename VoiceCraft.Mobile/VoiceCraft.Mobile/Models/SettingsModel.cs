namespace VoiceCraft.Mobile.Models
{
    public class SettingsModel
    {
        public int InputDevice { get; set; } = 0;
        public int OutputDevice { get; set; } = 0;
        public ushort WebsocketPort { get; set; } = 8080;
        public bool DirectionalAudioEnabled { get; set; }
        public bool PinpointPlayerAccuracyEnabled { get; set; }
    }
}
