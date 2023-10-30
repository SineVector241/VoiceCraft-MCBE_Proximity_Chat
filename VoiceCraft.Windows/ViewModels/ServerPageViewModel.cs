using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;
using VoiceCraft.Core.Client;
using VoiceCraft.Windows.Models;
using VoiceCraft.Windows.Storage;
using VoiceCraft.Windows.Views;

namespace VoiceCraft.Windows.ViewModels
{
    public partial class ServerPageViewModel : ObservableObject
    {
        [ObservableProperty]
        string externalServerInformation = "Pinging...";

        [ObservableProperty]
        ServerModel server;

        [ObservableProperty]
        SettingsModel settings;

        public ServerPageViewModel()
        {
            Server = Database.GetPassableObject<ServerModel>();
            Settings = Database.GetSettings();
            _ = Task.Run(async () => {
                var res = await VoiceCraftClient.PingAsync(Server.IP, Server.Port);
                ExternalServerInformation = res;
            });

            Database.OnServerUpdated += ServerUpdated;
        }

        private void ServerUpdated(ServerModel Server)
        {
            OnPropertyChanged(nameof(Server));
        }

        [RelayCommand]
        public void Connect()
        {
            Navigator.NavigateTo(new VoicePage());
        }

        [RelayCommand]
        public void Edit()
        {
            Navigator.NavigateTo(new EditPage());
        }

        [RelayCommand]
        public void Back()
        {
            Navigator.GoToPreviousPage();
            Database.OnServerUpdated -= ServerUpdated;
        }
    }
}
