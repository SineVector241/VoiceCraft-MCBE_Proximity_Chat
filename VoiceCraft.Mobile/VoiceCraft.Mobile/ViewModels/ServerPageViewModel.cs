using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;
using VoiceCraft.Mobile.Models;
using VoiceCraft.Mobile.Network;
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
        public async void Connect()
        {
            var granted = await Utils.CheckAndRequestPermissions();
            if(granted)
            {
                MessagingCenter.Send(new StartServiceMessage(), "ServiceStarted");
                await Shell.Current.GoToAsync(nameof(VoicePage));
            }
        }

        [RelayCommand]
        public void Edit()
        {
            Shell.Current.DisplayAlert("RickRoll", "Never gonna give you up. Never gonna let you down.", "Damnit");
        }

        [RelayCommand]
        public void Back()
        {
            Shell.Current.Navigation.PopAsync();
        }
    }
}
