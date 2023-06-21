using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;
using VoiceCraft.Windows.Models;
using VoiceCraft.Windows.Network;
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

        public ServerPageViewModel()
        {
            Server = Database.GetPassableObject<ServerModel>();
            _ = Task.Run(async () => {
                var res = await NetworkManager.InfoPingAsync(server.IP, server.Port);
                ExternalServerInformation = res;
            });
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
        }
    }
}
