using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VoiceCraft.Client;
using VoiceCraft.Maui.Services;
using VoiceCraft.Maui.Views.Desktop;
using VoiceCraft.Maui.Models;

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
            await Navigator.NavigateTo(nameof(Voice), Server);
        }
    }
}
