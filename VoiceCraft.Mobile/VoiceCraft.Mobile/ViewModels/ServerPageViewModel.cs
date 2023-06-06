using CommunityToolkit.Mvvm.ComponentModel;
using VoiceCraft.Mobile.Models;
using VoiceCraft.Mobile.Storage;
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
        }
    }
}
