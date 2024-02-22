using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System.Collections.ObjectModel;
using VoiceCraft.Core.Client;
using VoiceCraft.Maui.Models;
using VoiceCraft.Maui.Services;
using VoiceCraft.Models;

namespace VoiceCraft.Maui.ViewModels
{
    public partial class VoiceViewModel : ObservableObject
    {
        [ObservableProperty]
        string statusText = "Connecting...";

        [ObservableProperty]
        string passwordInput = string.Empty;

        [ObservableProperty]
        bool isMuted = false;

        [ObservableProperty]
        bool isDeafened = false;

        [ObservableProperty]
        bool isSpeaking = false;

        [ObservableProperty]
        bool showSlider = false;

        [ObservableProperty]
        bool showChannels = false;

        [ObservableProperty]
        bool showPasswordInput = false;

        [ObservableProperty]
        VoiceCraftParticipant? selectedParticipant;

        [ObservableProperty]
        VoiceCraftChannel? selectedChannel;

        [ObservableProperty]
        ObservableCollection<ParticipantModel> participants = new ObservableCollection<ParticipantModel>();

        [ObservableProperty]
        ObservableCollection<ChannelModel> channels = new ObservableCollection<ChannelModel>();

        public VoiceViewModel()
        {
            WeakReferenceMessenger.Default.Send<StartServiceMSG>();
            Preferences.Set("VoipServiceRunning", true);
        }

        [RelayCommand]
        public async Task OnPageAppearing()
        {
            if (Preferences.Get("VoipServiceRunning", false) == false)
            {
                await Navigator.GoBack();
                return;
            }

            WeakReferenceMessenger.Default.Register(this, (object recipient, StatusMessageUpdatedMSG message) =>
            {
                StatusText = message.Status;
            });

            WeakReferenceMessenger.Default.Register(this, (object recipient, SpeakingStatusChangedMSG message) =>
            {
                IsSpeaking = message.Status;
            });

            WeakReferenceMessenger.Default.Register(this, (object recipient, MutedStatusChangedMSG message) =>
            {
                IsMuted = message.Status;
            });

            WeakReferenceMessenger.Default.Register(this, (object recipient, DeafenedStatusChangedMSG message) =>
            {
                IsDeafened = message.Status;
            });

            WeakReferenceMessenger.Default.Register(this, (object recipient, ParticipantAddedMSG message) =>
            {
                if (Participants.FirstOrDefault(x => x.Participant == message.Participant) == null)
                {
                    Participants.Add(new ParticipantModel(message.Participant));
                }
            });

            WeakReferenceMessenger.Default.Register(this, (object recipient, ParticipantRemovedMSG message) =>
            {
                var displayParticipant = Participants.FirstOrDefault(x => x.Participant == message.Participant);
                if (displayParticipant != null)
                {
                    Participants.Remove(displayParticipant);
                }
            });

            WeakReferenceMessenger.Default.Register(this, (object recipient, ParticipantChangedMSG message) =>
            {
                var displayParticipant = Participants.FirstOrDefault(x => x.Participant == message.Participant);
                if (displayParticipant != null)
                {
                    displayParticipant.IsMuted = message.Participant.Muted;
                    displayParticipant.IsDeafened = message.Participant.Deafened;
                }
            });

            WeakReferenceMessenger.Default.Register(this, (object recipient, ParticipantSpeakingStatusChangedMSG message) =>
            {
                var displayParticipant = Participants.FirstOrDefault(x => x.Participant == message.Participant);
                if (displayParticipant != null)
                {
                    displayParticipant.IsSpeaking = message.Status;
                }
            });

            WeakReferenceMessenger.Default.Register(this, (object recipient, ChannelCreatedMSG message) =>

            {
                if (Channels.FirstOrDefault(x => x.Channel == message.Channel) == null)
                {
                    Channels.Add(new ChannelModel(message.Channel));
                }
            });

            WeakReferenceMessenger.Default.Register(this, (object recipient, ChannelEnteredMSG message) =>
            {
                var displayChannel = Channels.FirstOrDefault(x => x.Channel == message.Channel);
                if (displayChannel != null)
                {
                    displayChannel.Joined = true;
                }
            });

            WeakReferenceMessenger.Default.Register(this, (object recipient, ChannelLeftMSG message) =>
            {
                var displayChannel = Channels.FirstOrDefault(x => x.Channel == message.Channel);
                if (displayChannel != null)
                {
                    displayChannel.Joined = false;
                }
            });

            WeakReferenceMessenger.Default.Register(this, (object recipient, DisconnectedMSG message) =>
            {
                if (!string.IsNullOrWhiteSpace(message.Reason))
                    Shell.Current.DisplayAlert("Disconnected!", message.Reason, "OK");
                Shell.Current.Navigation.PopAsync();
            });

            WeakReferenceMessenger.Default.Register(this, (object recipient, DenyMSG message) =>
            {
                if (!string.IsNullOrWhiteSpace(message.Reason))
                    Shell.Current.DisplayAlert("Denied", message.Reason, "OK");
            });

            WeakReferenceMessenger.Default.Register(this, (object recipient, ResponseData message) =>

            {
                StatusText = message.StatusMessage;
                IsMuted = message.IsMuted;
                IsDeafened = message.IsDeafened;
                IsSpeaking = message.IsSpeaking;
                Participants = new ObservableCollection<ParticipantModel>(message.Participants);
                Channels = new ObservableCollection<ChannelModel>(message.Channels);
            });

            WeakReferenceMessenger.Default.Send<RequestData>();
        }

        [RelayCommand]
        public void OnPageDisappearing()
        {
            WeakReferenceMessenger.Default.UnregisterAll(this);
        }
    }
}
