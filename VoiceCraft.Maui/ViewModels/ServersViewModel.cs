using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using VoiceCraft.Maui.Services;
using VoiceCraft.Maui.Views.Desktop;
using VoiceCraft.Models;

namespace VoiceCraft.Maui.ViewModels
{
    public partial class ServersViewModel : ObservableObject
    {
        [ObservableProperty]
        ObservableCollection<ServerModel> servers = new ObservableCollection<ServerModel>(Database.Instance.Servers);

        [ObservableProperty]
        SettingsModel settings = Database.Instance.Settings;

        public ServersViewModel()
        {
            Database.Instance.OnServerAdded += ServerAdded;
            Database.Instance.OnServerRemoved += ServerRemoved;
        }

        [RelayCommand]
        public async Task DeleteServer(ServerModel server)
        {
            await Database.Instance.RemoveServer(server);
        }

        [RelayCommand]
        public async Task GoToAddServer()
        {
            await Navigator.NavigateTo(nameof(AddServer));
        }

        [RelayCommand]
        public async Task GoToEditServer(ServerModel server)
        {
            await Navigator.NavigateTo(nameof(EditServer), server);
        }

        private void ServerAdded(ServerModel server)
        {
            Servers.Add(server);
        }

        private void ServerRemoved(ServerModel server)
        {
            Servers.Remove(server);
        }
    }
}
