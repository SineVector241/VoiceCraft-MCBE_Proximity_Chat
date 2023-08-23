using CommunityToolkit.Mvvm.ComponentModel;
using VoiceCraft.Mobile.Network;

namespace VoiceCraft.Mobile.Models
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
