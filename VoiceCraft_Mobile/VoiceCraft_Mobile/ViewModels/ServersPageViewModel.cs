using CommunityToolkit.Mvvm.Input;
using VoiceCraft_Mobile.Views;
using VoiceCraft_Mobile.Repositories;
using System.Linq;

namespace VoiceCraft_Mobile.ViewModels
{
    public partial class ServersPageViewModel : BaseViewModel
    {
        [RelayCommand]
        async void GoToAddServer()
        {
            await App.Current.MainPage.Navigation.PushAsync(new AddServerPage());
        }

        [RelayCommand]
        void DeleteServer(string localId)
        {
            var server = Servers.FirstOrDefault(x => x.LocalId == localId);
            if (server != null)
            {
                Servers.Remove(server);
                Database.DeleteServer(server.LocalId);
                OnPropertyChanged(nameof(Servers));
            }
        }

        [RelayCommand]
        async void Connect(string localId)
        {
            var result = await Utils.CheckAndRequestPermissions();
            if (result == false) return;

            await App.Current.MainPage.Navigation.PushAsync(new VoicePage());
            var server = Database.GetServers().FirstOrDefault(x => x.LocalId == localId);
            if(server != null)
            {
                Network.Network.Current.signallingClient.Connect(server.Ip, server.Port, server.Id, localId);
            }
        }
    }
}
