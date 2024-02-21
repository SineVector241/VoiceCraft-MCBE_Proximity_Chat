using CommunityToolkit.Mvvm.ComponentModel;
using VoiceCraft.Maui.Services;
using VoiceCraft.Models;

namespace VoiceCraft.Maui.ViewModels
{
    public partial class ServerDetailsViewModel : ObservableObject
    {
        [ObservableProperty]
        ServerModel server;

        [ObservableProperty]
        SettingsModel settings = Database.Instance.Settings;

        public ServerDetailsViewModel()
        {
            server = Navigator.GetNavigationData<ServerModel>();
        }
    }
}
