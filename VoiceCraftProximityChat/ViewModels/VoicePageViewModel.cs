using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using VoiceCraftProximityChat.Services;
using VoiceCraftProximityChat.Views;

namespace VoiceCraftProximityChat.ViewModels
{
    public partial class VoicePageViewModel : ObservableObject
    {
        private VoipService voipService { get; set; } = new VoipService();
        private CancellationTokenSource cts { get; set; } = new CancellationTokenSource();

        [ObservableProperty]
        string statusText = "Connecting...";

        [ObservableProperty]
        bool isMuted = false;

        [ObservableProperty]
        ObservableCollection<string> participants = new ObservableCollection<string>();

        public VoicePageViewModel()
        {
            voipService.OnFailed += OnFailed;
            voipService.OnUpdate += OnUpdate;
        }

        private Task OnUpdate(UpdateUIMessage message)
        {
            if (StatusText != message.StatusMessage)
                StatusText = message.StatusMessage;

            var list = new ObservableCollection<string>();
            foreach (var part in message.Participants)
            {
                list.Add(part.Name);
            }
            if (list != Participants)
            {
                Participants = list;
            }
            return Task.CompletedTask;
        }

        private Task OnFailed(ServiceFailedMessage message)
        {
            cts.Cancel();
            App.Current.Dispatcher.Invoke(delegate
            {
                MessageBox.Show(message.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                var navigator = (Frame)App.Current.MainWindow.FindName("Navigator");
                navigator.Navigate(new ServersPage());
            });
            return Task.CompletedTask;
        }

        [RelayCommand]
        public void StartConnection(string serverName)
        {
            _ = Task.Run(() =>
            {
                try
                {
                    voipService.Run(cts.Token, serverName).Wait();
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
            var navigator = (Frame)App.Current.MainWindow.FindName("Navigator");
            navigator.Navigate(new ServersPage());
        }

        [RelayCommand]
        public void Mute()
        {
            IsMuted = !IsMuted;
            voipService.MuteUnmute(IsMuted);
        }
    }
}
