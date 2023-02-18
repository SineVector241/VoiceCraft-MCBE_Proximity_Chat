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
            await App.Current.MainPage.Navigation.PushAsync(new VoicePage(localId));
        }
    }
}
