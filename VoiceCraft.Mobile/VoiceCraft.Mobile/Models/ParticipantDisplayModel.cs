using CommunityToolkit.Mvvm.ComponentModel;
using VoiceCraft.Core.Client;

namespace VoiceCraft.Mobile.Models
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
        public VoiceCraftParticipant participant;

        public ParticipantDisplayModel(VoiceCraftParticipant participant)
        {
            this.participant = participant;
            isMuted = participant.Muted;
            isDeafened = participant.Deafened;
        }
    }
}
