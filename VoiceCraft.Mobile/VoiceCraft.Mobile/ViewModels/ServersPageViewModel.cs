using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using VoiceCraft.Mobile.Models;
using VoiceCraft.Mobile.Views;
using Xamarin.Forms;
using VoiceCraft.Mobile.Storage;

namespace VoiceCraft.Mobile.ViewModels
{
    public partial class ServersPageViewModel : ObservableObject
    {
        [ObservableProperty]
        ObservableCollection<ServerModel> servers = new ObservableCollection<ServerModel>(Database.GetServers());

        public ServersPageViewModel()
        {
            Database.OnServerAdd += ServerAdded;
            Database.OnServerRemove += ServerRemoved;
        }

        private void ServerAdded(ServerModel Server)
        {
            Servers.Add(Server);
        }

        private void ServerRemoved(ServerModel Server)
        {
            Servers.Remove(Server);
        }

        [RelayCommand]
        public async void GoToServer(ServerModel server)
        {
            Database.SetPassableObject(server);
            await Shell.Current.GoToAsync(nameof(ServerPage));
        }

        [RelayCommand]
        public async void GoToAddServer()
        {
            await Shell.Current.GoToAsync(nameof(AddServerPage));
        }

        [RelayCommand]
        public void DeleteServer(ServerModel server)
        {
            Database.DeleteServer(server);
        }
    }
}
