namespace VoiceCraft.Windows.Models
{
    public class SettingsModel
    {
        public int InputDevice { get; set; } = 0;
        public int OutputDevice { get; set; } = 0;
        public int WebsocketPort { get; set; } = 8080;
        public float SoftLimiterGain { get; set; } = 5.0f;
        public float MicrophoneDetectionPercentage { get; set; } = 0.04f;
        public bool DirectionalAudioEnabled { get; set; }
        public bool ClientSidedPositioning { get; set; }
        public bool LinearVolume { get; set; }
        public bool SoftLimiterEnabled { get; set; } = true;
        public bool HideAddress { get; set; }
        public string MuteKeybind { get; set; } = "LControlKey+M";
        public string DeafenKeybind { get; set; } = "LControlKey+LShiftKey+D";
    }
}
