using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VoiceCraft.Client;
using VoiceCraft.Maui.Services;
using VoiceCraft.Maui.Views.Desktop;
using VoiceCraft.Maui.Models;
using CommunityToolkit.Mvvm.Messaging;

namespace VoiceCraft.Maui.ViewModels
{
    public partial class ServerDetailsViewModel : ObservableObject
    {
        [ObservableProperty]
        ServerModel server;

        [ObservableProperty]
        SettingsModel settings = Database.Instance.Settings;

        [ObservableProperty]
        string pingDetails = "Pinging...";

        public ServerDetailsViewModel()
        {
            server = Navigator.GetNavigationData<ServerModel>();
            _ = Task.Run(async() => PingDetails = await VoiceCraftClient.PingAsync(server.IP, server.Port));
        }

        [RelayCommand]
        public async Task Connect()
        {
#if ANDROID
            var status = await Permissions.RequestAsync<Permissions.Microphone>();
            if (Permissions.ShouldShowRationale<Permissions.Microphone>())
            {
                await Shell.Current.DisplayAlert("Error", "VoiceCraft requires the microphone to communicate with other users!", "OK");
                return;
            }
            if (status != PermissionStatus.Granted) return;
#endif
            WeakReferenceMessenger.Default.Send(new StartServiceMSG());
            await Navigator.NavigateTo(nameof(Voice), Server);
        }
    }
}
