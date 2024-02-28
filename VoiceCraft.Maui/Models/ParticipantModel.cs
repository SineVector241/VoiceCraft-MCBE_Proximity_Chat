using CommunityToolkit.Mvvm.ComponentModel;
using VoiceCraft.Client;

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
            isMuted = participant.IsMuted;
            isDeafened = participant.IsDeafened;
            volume = participant.Volume;
        }

        partial void OnVolumeChanged(float value)
        {
            Participant.Volume = value;
        }
    }
}
