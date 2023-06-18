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

            //Not efficient but idc.
            foreach (var participant in Data.Participants)
                if (!Participants.Contains(participant))
                    Participants.Add(participant);

            foreach (var participant in Participants)
                if (!Data.Participants.Contains(participant))
                    Participants.Remove(participant);
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
            cts.Cancel();
        }

        [RelayCommand]
        public void MuteUnmute()
        {
            IsMuted = !IsMuted;
        }
    }
}
