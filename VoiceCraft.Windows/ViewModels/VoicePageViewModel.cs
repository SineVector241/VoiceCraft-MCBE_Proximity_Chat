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
using System.Diagnostics;

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
        bool isMuted = false;

        [ObservableProperty]
        bool isDeafened = false;

        [ObservableProperty]
        bool isSpeaking = false;

        [ObservableProperty]
        ParticipantDisplayModel selectedParticipant = new ParticipantDisplayModel();

        [ObservableProperty]
        bool showSlider = false;

        [ObservableProperty]
        bool showChannels = false;

        [ObservableProperty]
        ObservableCollection<ParticipantDisplayModel> participants = new ObservableCollection<ParticipantDisplayModel>();

        [ObservableProperty]
        ObservableCollection<ChannelDisplayModel> channels = new ObservableCollection<ChannelDisplayModel>()
        {
        };

        private List<System.Windows.Forms.Keys> PressedKeys = new List<System.Windows.Forms.Keys>();
        private string MuteKeybind = "Undefined";
        private string DeafenKeybind = "Undefined";

        public VoicePageViewModel()
        {
            voipService.OnServiceDisconnect += OnServiceDisconnect;
            voipService.OnUpdateStatus += OnUpdateStatus;
            voipService.OnUpdate += Update;

            Events.KeyDown += KeyDown;
            Events.KeyUp += KeyUp;
        }

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
                for(int i = 0; i < PressedKeys.Count; i++)
                {
                    combinedKeys += string.IsNullOrWhiteSpace(combinedKeys)? PressedKeys[i].ToString() : $"+{PressedKeys[i].ToString()}";
                }
                if(combinedKeys == MuteKeybind)
                {
                    IsMuted = !IsMuted;
                    voipService.Network.SetMute(IsMuted);
                }
                if(combinedKeys == DeafenKeybind)
                {
                    IsDeafened = !IsDeafened;
                    voipService.Network.SetDeafen(IsDeafened);
                }
            }
        }

        private void Update(UpdateMessage message)
        {
            for (int i = 0; i < message.Participants.Count; i++)
            {
                var participant = message.Participants[i];
                var displayParticipant = Participants.FirstOrDefault(x => x.Key == participant.Key);
                if (displayParticipant != null)
                {
                    displayParticipant.IsDeafened = participant.IsDeafened;
                    displayParticipant.IsMuted = participant.IsMuted;
                    displayParticipant.IsSpeaking = participant.IsSpeaking;
                }
                else
                {
                    Participants.Add(participant);
                }
            }

            for (int i = 0; i < Participants.Count; i++)
            {
                var participant = message.Participants.FirstOrDefault(x => x.Key == Participants[i].Key);
                if(participant == null)
                {
                    Participants.Remove(Participants[i]);
                }
            }

            IsSpeaking = message.IsSpeaking;
            IsDeafened = message.IsDeafened;
            IsMuted = message.IsMuted;
        }

        private void OnUpdateStatus(UpdateStatusMessage message)
        {
            StatusText = message.StatusMessage;
        }

        private void OnServiceDisconnect(string? Reason)
        {
            cts.Cancel();
            Events.Dispose();
            Events.KeyDown -= KeyDown;
            Events.KeyUp -= KeyUp;
            voipService.OnServiceDisconnect -= OnServiceDisconnect;
            voipService.OnUpdate -= Update;
            voipService.OnUpdateStatus -= OnUpdateStatus;
            if (!string.IsNullOrWhiteSpace(Reason))
                MessageBox.Show(Reason, "Disconnect", MessageBoxButton.OK, MessageBoxImage.Warning);
            Navigator.GoToPreviousPage();
        }

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
            Events.KeyDown -= KeyDown;
            Events.KeyUp -= KeyUp;
            voipService.OnServiceDisconnect -= OnServiceDisconnect;
            voipService.OnUpdate -= Update;
            voipService.OnUpdateStatus -= OnUpdateStatus;
            Navigator.GoToPreviousPage();
        }

        [RelayCommand]
        public void MuteUnmute()
        {
            IsMuted = !IsMuted;
            voipService.Network.SetMute(IsMuted);
        }

        [RelayCommand]
        public void DeafenUndeafen()
        {
            IsDeafened = !IsDeafened;
            voipService.Network.SetDeafen(IsDeafened);
        }

        [RelayCommand]
        public void ShowParticipantVolume(ushort key)
        {
            var participant = Participants.FirstOrDefault(x => x.Key == key);
            if(participant != null)
            {
                SelectedParticipant = participant;
                ShowSlider = true;
            }
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
    }
}
