using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using VoiceCraft_Android.Models;
using VoiceCraft_Android.Services;
using VoiceCraft_Android.Storage;
using VoiceCraft_Android.Views;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace VoiceCraft_Android.ViewModels
{
    public partial class MainPageViewModel : ObservableObject
    {
        [ObservableProperty]
        ObservableCollection<ServerModel> servers = new ObservableCollection<ServerModel>();

        [RelayCommand]
        public async Task GoToAddServer()
        {
            await App.Current.MainPage.Navigation.PushAsync(new AddServerPage());
        }

        [RelayCommand]
        public async Task Connect(string name)
        {
            var perm = await Utils.CheckAndRequestPermissions();
            if (!perm)
                return;

            if (Preferences.Get("VoipServiceRunning", false) == false)
            {
                StartService(name);
            }

            Utils.PushPage(new VoicePage());
        }

        [RelayCommand]
        public void DeleteServer(string name)
        {
            try
            {
                var server = Database.GetServerByName(name);
                Database.DeleteServer(server);
                Servers = new ObservableCollection<ServerModel>(Database.GetServers().Servers);
            }
            catch(InvalidOperationException ex)
            {
                Utils.DisplayAlert("Error", ex.Message);
            }
        }

        //Page codebehind to viewmodel communication
        [RelayCommand]
        public void OnAppearing()
        {
            Servers = new ObservableCollection<ServerModel>(Database.GetServers().Servers);
            OnPropertyChanged(nameof(Servers));
            if (Preferences.Get("VoipServiceRunning", false) == true)
            {
                Utils.PushPage(new VoicePage());
            }
        }

        private void StartService(string serverName)
        {
            var message = new StartServiceMessage() { ServerName = serverName };
            MessagingCenter.Send(message, "ServiceStarted");
            Preferences.Set("VoipServiceRunning", true);
        }
    }
}
