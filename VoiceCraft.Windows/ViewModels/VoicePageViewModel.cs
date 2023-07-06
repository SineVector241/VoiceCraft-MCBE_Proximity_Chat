using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using VoiceCraft.Windows.Services;
using CommunityToolkit.Mvvm.Input;
using VoiceCraft.Windows.Storage;
using System.Windows;

namespace VoiceCraft.Windows.ViewModels
{
    public partial class VoicePageViewModel : ObservableObject
    {
        private VoipService voipService = new VoipService();
        private CancellationTokenSource cts { get; set; } = new CancellationTokenSource();

        [ObservableProperty]
        string statusText = "Connecting...";

        [ObservableProperty]
        bool isMuted = false;

        [ObservableProperty]
        bool isDeafened = false;

        [ObservableProperty]
        bool isSpeaking = false;

        [ObservableProperty]
        ObservableCollection<string> participants = new ObservableCollection<string>();

        public VoicePageViewModel()
        {
            voipService.OnServiceDisconnect += OnServiceDisconnect;
            voipService.OnUpdate += OnUpdate;
        }

        private void OnUpdate(UpdateUIMessage Data)
        {
            if(StatusText != Data.StatusMessage)
                StatusText = Data.StatusMessage;

            if(IsMuted != Data.IsMuted)
                IsMuted = Data.IsMuted;

            if (IsDeafened != Data.IsDeafened)
                IsDeafened = Data.IsDeafened;

            if (IsSpeaking != Data.IsSpeaking)
                IsSpeaking = Data.IsSpeaking;

            Participants = new ObservableCollection<string>(Data.Participants);
        }

        private void OnServiceDisconnect(string? Reason)
        {
            cts.Cancel();
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
            voipService.SendDisconnectPacket = true;
            cts.Cancel();
            Navigator.GoToPreviousPage();
        }

        [RelayCommand]
        public void MuteUnmute()
        {
            IsMuted = !IsMuted;
            voipService.MuteUnmute();
        }

        [RelayCommand]
        public void DeafenUndeafen()
        {
            IsDeafened = !IsDeafened;
            voipService.DeafenUndeafen();
        }
    }
}
