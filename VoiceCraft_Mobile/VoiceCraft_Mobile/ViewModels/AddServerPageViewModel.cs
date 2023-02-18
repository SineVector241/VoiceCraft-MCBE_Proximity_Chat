using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Linq;
using VoiceCraft_Mobile.Models;
using VoiceCraft_Mobile.Repositories;

namespace VoiceCraft_Mobile.ViewModels
{
    public partial class AddServerPageViewModel : BaseViewModel
    {
        [ObservableProperty]
        ServerModel server = new ServerModel();

        [ObservableProperty]
        string errorMessage;

        [RelayCommand]
        async void Save()
        {
            if (string.IsNullOrWhiteSpace(Server.Name))
            {
                ErrorMessage = "Server Name cannot be empty.";
                return;
            }
            if (string.IsNullOrWhiteSpace(Server.Ip))
            {
                ErrorMessage = "IP Address cannot be empty.";
                return;
            }
            if (Server.Port < 1025 || Server.Port > 65535)
            {
                ErrorMessage = "Port cannot be lower than 1025 or higher than 65535";
                return;
            }
            Servers.Add(Server);
            Database.AddServer(Server);
            OnPropertyChanged(nameof(Servers));
            await App.Current.MainPage.Navigation.NavigationStack.LastOrDefault().Navigation.PopAsync();
        }
    }
}
