using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using VoiceCraft.Windows.Services;
using CommunityToolkit.Mvvm.Input;
using VoiceCraft.Windows.Storage;
using System.Windows;
using VoiceCraft.Windows.Models;
using System.Linq;
using Gma.System.MouseKeyHook;
using System.Collections.Generic;
using VoiceCraft.Core.Client;
using System.Drawing;

namespace VoiceCraft.Windows.ViewModels
{
    public partial class VoicePageViewModel : ObservableObject
    {
        private VoipService voipService = new VoipService();
        private CancellationTokenSource cts { get; set; } = new CancellationTokenSource();
        private IKeyboardMouseEvents Events { get; set; } = Hook.GlobalEvents();

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
        VoiceCraftParticipant? selectedParticipant;

        [ObservableProperty]
        VoiceCraftChannel? selectedChannel;

        [ObservableProperty]
        bool showSlider = false;

        [ObservableProperty]
        bool showChannels = false;

        [ObservableProperty]
        bool showPasswordInput = false;

        [ObservableProperty]
        ObservableCollection<ParticipantDisplayModel> participants = new ObservableCollection<ParticipantDisplayModel>();

        [ObservableProperty]
        ObservableCollection<ChannelDisplayModel> channels = new ObservableCollection<ChannelDisplayModel>();

        private List<System.Windows.Forms.Keys> PressedKeys = new List<System.Windows.Forms.Keys>();
        private string MuteKeybind = "Undefined";
        private string DeafenKeybind = "Undefined";

        public VoicePageViewModel()
        {
            voipService.OnStatusUpdated += StatusUpdated;
            voipService.OnSpeakingStatusChanged += SpeakingStatusChanged;
            voipService.OnMutedStatusChanged += MutedStatusChanged;
            voipService.OnDeafenedStatusChanged += DeafenedStatusChanged;
            voipService.OnParticipantAdded += ParticipantAdded;
            voipService.OnParticipantRemoved += ParticipantRemoved;
            voipService.OnParticipantSpeakingStatusChanged += ParticipantSpeakingStatusChanged;
            voipService.OnParticipantChanged += ParticipantChanged;
            voipService.OnChannelCreated += ChannelCreated;
            voipService.OnChannelEntered += ChannelEntered;
            voipService.OnChannelLeave += ChannelLeave;
            voipService.OnServiceDisconnected += OnServiceDisconnected;

            voipService.Network.Signalling.OnDenyPacketReceived += SignallingDeny;

            Events.KeyDown += KeyDown;
            Events.KeyUp += KeyUp;
        }

        #region Event Handlers
        private void KeyUp(object? sender, System.Windows.Forms.KeyEventArgs e)
        {
            PressedKeys.Remove(e.KeyCode);
        }

        private void KeyDown(object? sender, System.Windows.Forms.KeyEventArgs e)
        {
            if (!PressedKeys.Contains(e.KeyCode))
            {
                PressedKeys.Add(e.KeyCode);
                var combinedKeys = string.Empty;
                for (int i = 0; i < PressedKeys.Count; i++)
                {
                    combinedKeys += string.IsNullOrWhiteSpace(combinedKeys) ? PressedKeys[i].ToString() : $"+{PressedKeys[i].ToString()}";
                }
                if (combinedKeys == MuteKeybind)
                {
                    MuteUnmute();
                }
                if (combinedKeys == DeafenKeybind)
                {
                    DeafenUndeafen();
                }
            }
        }
        private void StatusUpdated(string status)
        {
            StatusText = status;
        }

        private void ChannelCreated(VoiceCraftChannel channel)
        {
            if (Channels.FirstOrDefault(x => x.Channel == channel) == null)
            {
                Channels.Add(new ChannelDisplayModel(channel));
            }
        }

        private void ChannelEntered(VoiceCraftChannel channel)
        {
            var displayChannel = Channels.FirstOrDefault(x => x.Channel == channel);
            if (displayChannel != null)
            {
                displayChannel.Joined = true;
            }
        }

        private void ChannelLeave(VoiceCraftChannel channel)
        {
            var displayChannel = Channels.FirstOrDefault(x => x.Channel == channel);
            if (displayChannel != null)
            {
                displayChannel.Joined = false;
            }
        }

        private void SpeakingStatusChanged(bool status)
        {
            IsSpeaking = status;
        }

        private void MutedStatusChanged(bool status)
        {
            IsMuted = status;
        }

        private void DeafenedStatusChanged(bool status)
        {
            IsDeafened = status;
        }

        private void ParticipantAdded(VoiceCraftParticipant participant)
        {
            if (Participants.FirstOrDefault(x => x.Participant == participant) == null)
            {
                Participants.Add(new ParticipantDisplayModel(participant));
            }
        }

        private void ParticipantRemoved(VoiceCraftParticipant participant)
        {
            var displayParticipant = Participants.FirstOrDefault(x => x.Participant == participant);
            if (displayParticipant != null)
            {
                Participants.Remove(displayParticipant);
            }
        }

        private void ParticipantSpeakingStatusChanged(VoiceCraftParticipant participant, bool status)
        {
            var displayParticipant = Participants.FirstOrDefault(x => x.Participant == participant);
            if (displayParticipant != null)
            {
                displayParticipant.IsSpeaking = status;
            }
        }

        private void ParticipantChanged(VoiceCraftParticipant participant)
        {
            var displayParticipant = Participants.FirstOrDefault(x => x.Participant == participant);
            if (displayParticipant != null)
            {
                displayParticipant.IsMuted = participant.Muted;
                displayParticipant.IsDeafened = participant.Deafened;
            }
        }

        private void SignallingDeny(Core.Packets.Signalling.Deny packet)
        {
            if(!packet.Disconnect)
            {
                MessageBox.Show(packet.Reason, "Denied", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void OnServiceDisconnected(string? Reason)
        {
            if (!string.IsNullOrWhiteSpace(Reason))
                MessageBox.Show(Reason, "Disconnect", MessageBoxButton.OK, MessageBoxImage.Warning);
            Disconnect();
        }
        #endregion

        #region Relay Commands
        [RelayCommand]
        public void StartConnection()
        {
            _ = Task.Run(() =>
            {
                try
                {
                    var settings = Database.GetSettings();
                    MuteKeybind = settings.MuteKeybind;
                    DeafenKeybind = settings.DeafenKeybind;
                    voipService.Start(cts.Token).Wait();
                }
                catch
                {
                    cts.Cancel();
                }
            }, cts.Token);
        }

        [RelayCommand]
        public void Disconnect()
        {
            cts.Cancel();
            Events.Dispose();

            voipService.OnStatusUpdated -= StatusUpdated;
            voipService.OnSpeakingStatusChanged -= SpeakingStatusChanged;
            voipService.OnMutedStatusChanged -= MutedStatusChanged;
            voipService.OnDeafenedStatusChanged -= DeafenedStatusChanged;
            voipService.OnParticipantAdded -= ParticipantAdded;
            voipService.OnParticipantRemoved -= ParticipantRemoved;
            voipService.OnParticipantSpeakingStatusChanged -= ParticipantSpeakingStatusChanged;
            voipService.OnParticipantChanged -= ParticipantChanged;
            voipService.OnChannelCreated -= ChannelCreated;
            voipService.OnServiceDisconnected -= OnServiceDisconnected;

            Events.KeyDown -= KeyDown;
            Events.KeyUp -= KeyUp;
            Navigator.GoToPreviousPage();
        }

        [RelayCommand]
        public void MuteUnmute()
        {
            voipService.Network.SetMute();
        }

        [RelayCommand]
        public void DeafenUndeafen()
        {
            voipService.Network.SetDeafen();
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
        }

        [RelayCommand]
        public void JoinLeaveChannel(VoiceCraftChannel channel)
        {
            if (channel.Joined)
            {
                voipService.Network.LeaveChannel(channel);
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
                    voipService.Network.JoinChannel(channel);
                }
            }
        }

        [RelayCommand]
        public void JoinChannel()
        {
            if (string.IsNullOrWhiteSpace(PasswordInput))
            {
                MessageBox.Show("Password cannot be empty or whitespace!", "Password Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if(SelectedChannel != null)
            {
                ShowPasswordInput = false;
                voipService.Network.JoinChannel(SelectedChannel, PasswordInput);
            }
        }

        [RelayCommand]
        public void HidePasswordInput()
        {
            ShowPasswordInput = false;
        }
        #endregion
    }
}
