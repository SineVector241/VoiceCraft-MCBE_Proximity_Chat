using CommunityToolkit.Mvvm.ComponentModel;

namespace VoiceCraft.Models
{
    public partial class SettingsModel : ObservableObject
    {
        [ObservableProperty]
        int inputDevice = 0;
        [ObservableProperty]
        int outputDevice = 0;
        [ObservableProperty]
        int websocketPort = 8080;
        [ObservableProperty]
        float softLimiterGain = 5.0f;
        [ObservableProperty]
        float microphoneDetectionPercentage = 0.04f;
        [ObservableProperty]
        bool directionalAudioEnabled;
        [ObservableProperty]
        bool clientSidedPositioning;
        [ObservableProperty]
        bool linearVolume;
        [ObservableProperty]
        bool softLimiterEnabled = true;
        [ObservableProperty]
        bool hideAddress;
        [ObservableProperty]
        string muteKeybind = "LControlKey+M";
        [ObservableProperty]
        string deafenKeybind = "LControlKey+LShiftKey+D";
    }
}
