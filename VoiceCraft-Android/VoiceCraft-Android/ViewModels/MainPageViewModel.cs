using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using VoiceCraft_Android.Models;
using VoiceCraft_Android.Storage;
using VoiceCraft_Android.Views;
using Xamarin.Essentials;
using Xamarin.Forms;
using static VoiceCraft_Android.Services.Messages;

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

            if(Preferences.Get("VoipServiceRunning", false) == false)
            {
                StartService(name);
            }
            else
            {
                StopService();
            }
        }

        [RelayCommand]
        public async Task DeleteServer(string name)
        {
            try
            {
                var server = Database.GetServerByName(name);
                Database.DeleteServer(server);
                Servers = new ObservableCollection<ServerModel>(Database.GetServers().Servers);
            }
            catch(InvalidOperationException ex)
            {
                await Utils.DisplayAlertAsync("Error", ex.Message);
            }
        }

        public MainPageViewModel()
        {
            App.Current.PageAppearing += Appearing;
        }

        private void Appearing(object sender, Page e)
        {
            Servers = new ObservableCollection<ServerModel>(Database.GetServers().Servers);
            OnPropertyChanged(nameof(Servers));
        }

        private void StartService(string serverName)
        {
            var message = new StartServiceMessage() { ServerName = serverName };
            MessagingCenter.Send(message, "ServiceStarted");
            Preferences.Set("VoipServiceRunning", true);
        }

        private void StopService()
        {
            var message = new StopServiceMessage();
            MessagingCenter.Send(message, "ServiceStopped");
            Preferences.Set("VoipServiceRunning", false);
        }
    }
}
