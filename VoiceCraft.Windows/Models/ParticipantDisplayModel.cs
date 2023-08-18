using CommunityToolkit.Mvvm.ComponentModel;

namespace VoiceCraft.Windows.Models
{
    public partial class ParticipantDisplayModel : ObservableObject
    {
        [ObservableProperty]
        public string name = "";
        [ObservableProperty]
        public bool isSpeaking;
        [ObservableProperty]
        public ushort key;
        [ObservableProperty]
        public float volume;
    }
}
