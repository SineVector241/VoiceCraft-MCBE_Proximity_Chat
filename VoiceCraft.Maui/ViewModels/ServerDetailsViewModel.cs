using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VoiceCraft.Maui.Services;
using VoiceCraft.Maui.Views.Desktop;
using VoiceCraft.Maui.Models;
using CommunityToolkit.Mvvm.Messaging;
using VoiceCraft.Maui.VoiceCraft;

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
            var settings = Database.Instance.Settings;
            if (!AudioManager.Instance.RequestInputPermissions()) return;
            if (settings.ClientPort < 1025 || settings.ClientPort > 65535)
            {
                settings.ClientPort = 8080;
                await Database.Instance.SaveSettings();
            }

            if (Settings.InputDevice > AudioManager.Instance.GetInputDeviceCount())
            {
                Settings.InputDevice = 0;
            }
            if (Settings.OutputDevice > AudioManager.Instance.GetOutputDeviceCount())
            {
                Settings.OutputDevice = 0;
            }

            WeakReferenceMessenger.Default.Send(new StartServiceMSG());
            await Navigator.NavigateTo(nameof(Voice), Server);
        }
    }
}
