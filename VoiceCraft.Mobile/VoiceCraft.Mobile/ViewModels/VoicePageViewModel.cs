using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using VoiceCraft.Core.Client;
using VoiceCraft.Mobile.Models;
using VoiceCraft.Mobile.Services;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace VoiceCraft.Mobile.ViewModels
{
    public partial class VoicePageViewModel : ObservableObject
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
        ObservableCollection<ParticipantDisplayModel> participants = new ObservableCollection<ParticipantDisplayModel>();

        [ObservableProperty]
        ObservableCollection<ChannelDisplayModel> channels = new ObservableCollection<ChannelDisplayModel>();

        //Page codebehind to viewmodel.
        [RelayCommand]
        public void OnAppearing()
        {
            if (Preferences.Get("VoipServiceRunning", false) == false)
            {
                Device.BeginInvokeOnMainThread(() => {
                    Shell.Current.Navigation.PopAsync();
                });
                return;
            }

            MessagingCenter.Subscribe<StatusMessageUpdatedMSG>(this, "StatusMessageUpdated", message =>
            {
                StatusText = message.Status;
            });

            MessagingCenter.Subscribe<SpeakingStatusChangedMSG>(this, "SpeakingStatusChanged", message =>
            {
                IsSpeaking = message.Status;
            });

            MessagingCenter.Subscribe<MutedStatusChangedMSG>(this, "MutedStatusChanged", message =>
            {
                IsMuted = message.Status;
            });

            MessagingCenter.Subscribe<DeafenedStatusChangedMSG>(this, "DeafenedStatusChanged", message =>
            {
                IsDeafened = message.Status;
            });

            MessagingCenter.Subscribe<ParticipantAddedMSG>(this, "ParticipantAdded", message =>
            {
                if (Participants.FirstOrDefault(x => x.Participant == message.Participant) == null)
                {
                    Participants.Add(new ParticipantDisplayModel(message.Participant));
                }
            });

            MessagingCenter.Subscribe<ParticipantRemovedMSG>(this, "ParticipantRemoved", message =>
            {
                var displayParticipant = Participants.FirstOrDefault(x => x.Participant == message.Participant);
                if (displayParticipant != null)
                {
                    Participants.Remove(displayParticipant);
                }
            });
            
            MessagingCenter.Subscribe<ParticipantChangedMSG>(this, "ParticipantChanged", message =>
            {
                var displayParticipant = Participants.FirstOrDefault(x => x.Participant == message.Participant);
                if (displayParticipant != null)
                {
                    displayParticipant.IsMuted = message.Participant.Muted;
                    displayParticipant.IsDeafened = message.Participant.Deafened;
                }
            });

            MessagingCenter.Subscribe<ParticipantSpeakingStatusChangedMSG>(this, "ParticipantSpeakingStatusChanged", message =>
            {
                var displayParticipant = Participants.FirstOrDefault(x => x.Participant == message.Participant);
                if (displayParticipant != null)
                {
                    displayParticipant.IsSpeaking = message.Status;
                }
            });

            MessagingCenter.Subscribe<ChannelCreatedMSG>(this, "ChannelCreated", message =>
            {
                if (Channels.FirstOrDefault(x => x.Channel == message.Channel) == null)
                {
                    Channels.Add(new ChannelDisplayModel(message.Channel));
                }
            });

            MessagingCenter.Subscribe<ChannelEnteredMSG>(this, "ChannelEntered", message =>
            {
                var displayChannel = Channels.FirstOrDefault(x => x.Channel == message.Channel);
                if (displayChannel != null)
                {
                    displayChannel.Joined = true;
                }
            });

            MessagingCenter.Subscribe<ChannelLeftMSG>(this, "ChannelLeft", message =>
            {
                var displayChannel = Channels.FirstOrDefault(x => x.Channel == message.Channel);
                if (displayChannel != null)
                {
                    displayChannel.Joined = false;
                }
            });

            MessagingCenter.Subscribe<DisconnectedMSG>(this, "Disconnected", message =>
            {
                if (!string.IsNullOrWhiteSpace(message.Reason))
                    Shell.Current.DisplayAlert("Disconnected!", message.Reason, "OK");
                Shell.Current.Navigation.PopAsync();
            });

            MessagingCenter.Subscribe<DenyMSG>(this, "Deny", message =>
            {
                if (!string.IsNullOrWhiteSpace(message.Reason))
                    Shell.Current.DisplayAlert("Denied", message.Reason, "OK");
            });

            MessagingCenter.Subscribe<ResponseData>(this, "ResponseData", message =>
            {
                StatusText = message.StatusMessage;
                IsMuted = message.IsMuted;
                IsDeafened = message.IsDeafened;
                IsSpeaking = message.IsSpeaking;
                Participants = new ObservableCollection<ParticipantDisplayModel>(message.Participants);
                Channels = new ObservableCollection<ChannelDisplayModel>(message.Channels);
            });

            MessagingCenter.Send(new RequestData(), "RequestData");
        }

        [RelayCommand]
        public void OnDisappearing()
        {
            MessagingCenter.Unsubscribe<StatusMessageUpdatedMSG>(this, "StatusMessageUpdated");
            MessagingCenter.Unsubscribe<SpeakingStatusChangedMSG>(this, "SpeakingStatusChanged");
            MessagingCenter.Unsubscribe<MutedStatusChangedMSG>(this, "MutedStatusChanged");
            MessagingCenter.Unsubscribe<DeafenedStatusChangedMSG>(this, "DeafenedStatusChanged");
            MessagingCenter.Unsubscribe<ParticipantAddedMSG>(this, "ParticipantAdded");
            MessagingCenter.Unsubscribe<ParticipantRemovedMSG>(this, "ParticipantRemoved");
            MessagingCenter.Unsubscribe<ParticipantChangedMSG>(this, "ParticipantChanged");
            MessagingCenter.Unsubscribe<ParticipantSpeakingStatusChangedMSG>(this, "ParticipantSpeakingStatusChanged");
            MessagingCenter.Unsubscribe<ChannelCreatedMSG>(this, "ChannelCreated");
            MessagingCenter.Unsubscribe<ChannelEnteredMSG>(this, "ChannelEntered");
            MessagingCenter.Unsubscribe<ChannelLeftMSG>(this, "ChannelLeft");
            MessagingCenter.Unsubscribe<DisconnectedMSG>(this, "Disconnected");
            MessagingCenter.Unsubscribe<DenyMSG>(this, "Deny");
            MessagingCenter.Unsubscribe<ResponseData>(this, "Response");
        }

        [RelayCommand]
        public void MuteUnmute()
        {
            MessagingCenter.Send(new MuteUnmuteMSG(), "MuteUnmute");
        }

        [RelayCommand]
        public void DeafenUndeafen()
        {
            MessagingCenter.Send(new DeafenUndeafenMSG(), "DeafenUndeafen");
        }

        [RelayCommand]
        public void Disconnect()
        {
            MessagingCenter.Send(new DisconnectMSG(), "Disconnect");
            Shell.Current.Navigation.PopAsync();
        }

        [RelayCommand]
        public void ShowParticipantVolume(VoiceCraftParticipant participant)
        {
            SelectedParticipant = participant;
            ShowSlider = true;
        }

        [RelayCommand]
        public void HideParticipantVolume()
        {
            ShowSlider = false;
        }

        [RelayCommand]
        public void ToggleChannelVisibility()
        {
            ShowChannels = !ShowChannels;
            if(ShowChannels == false)
                ShowPasswordInput = false;
        }

        [RelayCommand]
        public void JoinLeaveChannel(VoiceCraftChannel channel)
        {
            if (channel.Joined)
            {
                MessagingCenter.Send(new LeaveChannelMSG(channel), "LeaveChannel");
            }
            else
            {
                if (channel.RequiresPassword)
                {
                    PasswordInput = string.Empty;
                    SelectedChannel = channel;
                    ShowPasswordInput = true;
                }
                else
                {
                    MessagingCenter.Send(new JoinChannelMSG(channel), "JoinChannel");
                }
            }
        }

        [RelayCommand]
        public void JoinChannel()
        {
            if (string.IsNullOrWhiteSpace(PasswordInput))
            {
                Shell.Current.DisplayAlert("Password Error!", "Password cannot be empty or whitespace!", "OK");
                return;
            }
            if (SelectedChannel != null)
            {
                ShowPasswordInput = false;
                MessagingCenter.Send(new JoinChannelMSG(SelectedChannel) { Password = PasswordInput }, "JoinChannel");
            }
        }

        [RelayCommand]
        public void HidePasswordInput()
        {
            ShowPasswordInput = false;
        }
    }
}
