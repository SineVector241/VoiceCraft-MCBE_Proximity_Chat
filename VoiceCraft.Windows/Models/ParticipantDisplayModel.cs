using CommunityToolkit.Mvvm.ComponentModel;
using VoiceCraft.Core.Client;

namespace VoiceCraft.Windows.Models
{
    public partial class ParticipantDisplayModel : ObservableObject
    {
        [ObservableProperty]
        public bool isSpeaking;
        [ObservableProperty]
        public bool isMuted;
        [ObservableProperty]
        public bool isDeafened;
        [ObservableProperty]
        public ushort key;
        [ObservableProperty]
        public VoiceCraftParticipant? participant;
    }
}
