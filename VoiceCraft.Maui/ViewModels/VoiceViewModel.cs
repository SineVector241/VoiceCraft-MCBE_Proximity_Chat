using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System.Collections.ObjectModel;
using VoiceCraft.Client;
using VoiceCraft.Maui.Models;
using VoiceCraft.Maui.Services;

namespace VoiceCraft.Maui.ViewModels
{
    public partial class VoiceViewModel : ObservableObject
    {
        [ObservableProperty]
        string statusText = "Connecting...";

        [ObservableProperty]
        string passwordInput = string.Empty;

        [ObservableProperty]
        ParticipantModel? selectedParticipant;

        [ObservableProperty]
        bool isMuted = false;

        [ObservableProperty]
        bool isDeafened = false;

        [ObservableProperty]
        bool isSpeaking = false;

        [ObservableProperty]
        bool showChannels = false;

        [ObservableProperty]
        bool showParticipantVolume = false;

        [ObservableProperty]
        ObservableCollection<ParticipantModel> participants = new ObservableCollection<ParticipantModel>();

        [ObservableProperty]
        ObservableCollection<ChannelModel> channels = new ObservableCollection<ChannelModel>();

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
                StatusText = message.Value;
            });

            WeakReferenceMessenger.Default.Register(this, (object recipient, SpeakingStatusChangedMSG message) =>
            {
                IsSpeaking = message.Value;
            });

            WeakReferenceMessenger.Default.Register(this, (object recipient, MutedStatusChangedMSG message) =>
            {
                IsMuted = message.Value;
            });

            WeakReferenceMessenger.Default.Register(this, (object recipient, DeafenedStatusChangedMSG message) =>
            {
                IsDeafened = message.Value;
            });

            WeakReferenceMessenger.Default.Register(this, (object recipient, ParticipantAddedMSG message) =>
            {
                if (Participants.FirstOrDefault(x => x.Participant == message.Value) == null)
                {
                    Participants.Add(new ParticipantModel(message.Value));
                }
            });

            WeakReferenceMessenger.Default.Register(this, (object recipient, ParticipantRemovedMSG message) =>
            {
                var displayParticipant = Participants.FirstOrDefault(x => x.Participant == message.Value);
                if (displayParticipant != null)
                {
                    Participants.Remove(displayParticipant);
                }
            });

            WeakReferenceMessenger.Default.Register(this, (object recipient, ParticipantChangedMSG message) =>
            {
                var displayParticipant = Participants.FirstOrDefault(x => x.Participant == message.Value);
                if (displayParticipant != null)
                {
                    displayParticipant.IsMuted = message.Value.IsMuted;
                    displayParticipant.IsDeafened = message.Value.IsDeafened;
                }
            });

            WeakReferenceMessenger.Default.Register(this, (object recipient, ParticipantSpeakingStatusChangedMSG message) =>
            {
                var displayParticipant = Participants.FirstOrDefault(x => x.Participant == message.Value.Participant);
                if (displayParticipant != null)
                {
                    displayParticipant.IsSpeaking = message.Value.Status;
                }
            });

            WeakReferenceMessenger.Default.Register(this, (object recipient, ChannelCreatedMSG message) =>

            {
                if (Channels.FirstOrDefault(x => x.Channel == message.Value) == null)
                {
                    Channels.Add(new ChannelModel(message.Value));
                }
            });

            WeakReferenceMessenger.Default.Register(this, (object recipient, ChannelEnteredMSG message) =>
            {
                var displayChannel = Channels.FirstOrDefault(x => x.Channel == message.Value);
                if (displayChannel != null)
                {
                    displayChannel.Joined = true;
                }
            });

            WeakReferenceMessenger.Default.Register(this, (object recipient, ChannelLeftMSG message) =>
            {
                var displayChannel = Channels.FirstOrDefault(x => x.Channel == message.Value);
                if (displayChannel != null)
                {
                    displayChannel.Joined = false;
                }
            });

            WeakReferenceMessenger.Default.Register(this, async (object recipient, DisconnectedMSG message) =>
            {
                if (!string.IsNullOrWhiteSpace(message.Value))
                    await Shell.Current.DisplayAlert("Disconnected!", message.Value, "OK");
                await Navigator.GoBack();
            });

            WeakReferenceMessenger.Default.Register(this, (object recipient, DenyMSG message) =>
            {
                if (!string.IsNullOrWhiteSpace(message.Value))
                    Shell.Current.DisplayAlert("Denied", message.Value, "OK");
            });

            WeakReferenceMessenger.Default.Register(this, (object recipient, ResponseDataMSG message) =>

            {
                StatusText = message.Value.StatusMessage;
                IsMuted = message.Value.IsMuted;
                IsDeafened = message.Value.IsDeafened;
                IsSpeaking = message.Value.IsSpeaking;
                Participants = new ObservableCollection<ParticipantModel>(message.Value.Participants);
                Channels = new ObservableCollection<ChannelModel>(message.Value.Channels);
            });

            WeakReferenceMessenger.Default.Send(new RequestDataMSG());
        }

        [RelayCommand]
        public void OnPageDisappearing()
        {
            WeakReferenceMessenger.Default.UnregisterAll(this);
        }

        [RelayCommand]
        public async Task Disconnect()
        {
            WeakReferenceMessenger.Default.Send(new DisconnectMSG());
            Preferences.Set("VoipServiceRunning", false);
            await Navigator.GoBack();
        }

        [RelayCommand]
        public void MuteUnmute()
        {
            WeakReferenceMessenger.Default.Send(new MuteUnmuteMSG());
        }

        [RelayCommand]
        public void DeafenUndeafen()
        {
            WeakReferenceMessenger.Default.Send(new DeafenUndeafenMSG());
        }

        [RelayCommand]
        public void ShowHideChannels()
        {
            ShowChannels = !ShowChannels;
        }

        [RelayCommand]
        public void ShowVolume(ParticipantModel participant)
        {
            SelectedParticipant = participant;
            ShowParticipantVolume = true;
        }

        [RelayCommand]
        public void HideVolume()
        {
            ShowParticipantVolume = false;
        }

        [RelayCommand]
        public async Task JoinChannel(VoiceCraftChannel channel)
        {
            if (channel.Joined)
            {
                WeakReferenceMessenger.Default.Send(new LeaveChannelMSG(new LeaveChannel(channel)));
            }
            else
            {
                if (channel.RequiresPassword)
                {
                    var res = await Shell.Current.DisplayPromptAsync("Password", "Please input a password for the channel.", maxLength: 12);
                    if(!string.IsNullOrWhiteSpace(res))
                    {
                        WeakReferenceMessenger.Default.Send(new JoinChannelMSG(new JoinChannel(channel) { Password = res }));
                    }
                }
                else
                {
                    WeakReferenceMessenger.Default.Send(new JoinChannelMSG(new JoinChannel(channel)));
                }
            }
        }
    }
}
