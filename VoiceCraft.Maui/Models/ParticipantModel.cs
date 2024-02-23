using CommunityToolkit.Mvvm.ComponentModel;
using VoiceCraft.Core.Client;

namespace VoiceCraft.Maui.Models
{
    public partial class ParticipantModel : ObservableObject
    {
        [ObservableProperty]
        bool isSpeaking;
        [ObservableProperty]
        bool isMuted;
        [ObservableProperty]
        bool isDeafened;
        [ObservableProperty]
        float volume;
        [ObservableProperty]
        VoiceCraftParticipant participant;

        public ParticipantModel(VoiceCraftParticipant participant)
        {
            this.participant = participant;
            isMuted = participant.Muted;
            isDeafened = participant.Deafened;
            volume = participant.Volume;
        }

        partial void OnVolumeChanged(float value)
        {
            Participant.Volume = value;
        }
    }
}
