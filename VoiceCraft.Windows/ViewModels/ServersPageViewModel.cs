using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Linq;
using VoiceCraft.Windows.Models;
using VoiceCraft.Windows.Storage;
using VoiceCraft.Windows.Views;

namespace VoiceCraft.Windows.ViewModels
{
    public partial class ServersPageViewModel : ObservableObject
    {
        [ObservableProperty]
        ObservableCollection<ServerModel> servers = new ObservableCollection<ServerModel>(Database.GetServers());

        public ServersPageViewModel()
        {
            Database.OnServerAdd += ServerAdded;
            Database.OnServerUpdated += ServerUpdated;
            Database.OnServerRemove += ServerRemoved;
        }

        private void ServerUpdated(ServerModel Server)
        {
            Servers = new ObservableCollection<ServerModel>(Database.GetServers());
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
        public void GoToServer(ServerModel server)
        {
            Database.SetPassableObject(server);
            Navigator.NavigateTo(new ServerPage());
        }

        [RelayCommand]
        public void GoToAddServer()
        {
            Navigator.NavigateTo(new AddServerPage());
        }

        [RelayCommand]
        public void DeleteServer(ServerModel server)
        {
            Database.DeleteServer(server);
        }

        [RelayCommand]
        public void GoToSettings()
        {
            Navigator.NavigateTo(new SettingsPage());
        }

        [RelayCommand]
        public void GoToHelp()
        {
            Navigator.NavigateTo(new HelpPage());
        }

        [RelayCommand]
        public void GoToCredits()
        {
            Navigator.NavigateTo(new CreditsPage());
        }
    }
}
