using CommunityToolkit.Mvvm.ComponentModel;
using VoiceCraft.Windows.Network;

namespace VoiceCraft.Windows.Models
{
    public partial class ParticipantDisplayModel : ObservableObject
    {
        [ObservableProperty]
        public bool isSpeaking;
        [ObservableProperty]
        public ushort key;
        [ObservableProperty]
        public VoiceCraftParticipant? participant;
    }
}
