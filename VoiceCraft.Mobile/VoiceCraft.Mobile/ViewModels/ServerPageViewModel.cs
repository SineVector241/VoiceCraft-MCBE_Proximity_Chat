using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;
using VoiceCraft.Core.Client;
using VoiceCraft.Mobile.Models;
using VoiceCraft.Mobile.Services;
using VoiceCraft.Mobile.Storage;
using VoiceCraft.Mobile.Views;
using Xamarin.Forms;

namespace VoiceCraft.Mobile.ViewModels
{
    public partial class ServerPageViewModel : ObservableObject
    {
        [ObservableProperty]
        string externalServerInformation = "Pinging...";

        [ObservableProperty]
        ServerModel? server;

        [ObservableProperty]
        SettingsModel? settings;

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
            this.Server = Server;
        }

        [RelayCommand]
        public async void Connect()
        {
            var granted = await Utils.CheckAndRequestPermissions();
            if(granted)
            {
                MessagingCenter.Send(new StartServiceMSG(), "StartService");
                await Shell.Current.GoToAsync(nameof(VoicePage));
            }
        }

        [RelayCommand]
        public void Edit()
        {
            Shell.Current.GoToAsync(nameof(EditPage));
        }

        [RelayCommand]
        public void Back()
        {
            Shell.Current.Navigation.PopAsync();
            Database.OnServerUpdated -= ServerUpdated;
        }
    }
}
