using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Linq;
using VoiceCraft_Mobile.Models;
using VoiceCraft_Mobile.Repositories;
using VoiceCraft_Mobile.Views;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Internals;

namespace VoiceCraft_Mobile.ViewModels
{
    public partial class VoicePageViewModel : BaseViewModel
    {
        [ObservableProperty]
        string statusMessage = "Connecting...";

        public VoicePageViewModel()
        {
            App.Current.PageAppearing += OnPageAppearing;
            App.Current.PageDisappearing += OnPageDisappearing;
        }

        [RelayCommand]
        async void Disconnect()
        {
            Network.Network.Current.Disconnect();
            await Utils.GoToPreviousPageAsync();
        }

        public async void OnPageAppearing(object sender, Page e)
        {
            if(e is VoicePage)
            {
                var server = Database.GetServers().FirstOrDefault(x => x.LocalId == Network.Network.Current.localServerId);
                var result = await Network.Network.Current.ConnectAndLoginAsync(server.Ip, server.Port, server.Id);
                if (result)
                {
                    server.Id = Network.Network.Current.loginId;
                    Database.EditServer(server);

                    for(var i = 0; i < Servers.Count; i++)
                    {
                        if (Servers[i].LocalId == server.LocalId)
                        {
                            Servers[i] = server;
                            break;
                        }
                    }
                    OnPropertyChanged(nameof(Servers));

                    StatusMessage = $"Connected, Key: {Network.Network.Current.loginId}\nWaiting for binding...";
                }
                else
                    await Utils.GoToPreviousPageAsync();
            }
        }

        public void OnPageDisappearing(object sender, Page e)
        {
            if(e is VoicePage)
            {
                App.Current.PageDisappearing -= OnPageDisappearing;
                App.Current.PageAppearing -= OnPageAppearing;
            }
        }
    }
}
