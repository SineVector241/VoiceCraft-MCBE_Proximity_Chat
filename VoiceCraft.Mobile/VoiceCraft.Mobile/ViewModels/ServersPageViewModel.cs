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

        [ObservableProperty]
        SettingsModel settings = Database.GetSettings();

        public ServersPageViewModel()
        {
            Database.OnServerAdd += ServerAdded;
            Database.OnServerUpdated += ServerUpdated;
            Database.OnServerRemove += ServerRemoved;
            Database.OnSettingsUpdated += SettingsUpdated;
        }

        private void SettingsUpdated(SettingsModel Settings)
        {
            this.Settings = Settings;
        }

        private void ServerAdded(ServerModel Server)
        {
            Servers.Add(Server);
        }

        private void ServerUpdated(ServerModel Server)
        {
            Servers = new ObservableCollection<ServerModel>(Database.GetServers());
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
