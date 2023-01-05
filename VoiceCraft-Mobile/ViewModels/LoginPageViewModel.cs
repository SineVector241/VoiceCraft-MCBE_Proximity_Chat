using Microsoft.Toolkit.Mvvm.Input;
using VoiceCraft_Mobile.Network;
using VoiceCraft_Mobile.Views;
using System.Net;
using Microsoft.Toolkit.Mvvm.ComponentModel;

namespace VoiceCraft_Mobile.ViewModels
{
    public partial class LoginPageViewModel : BaseViewModel
    {
        [ObservableProperty]
        private string _ip;

        [ObservableProperty]
        private string _port;

        [ObservableProperty]
        private string _key;

        [ObservableProperty]
        private string _connectBtnMessage = "Connect";

        [ICommand]
        async void Login()
        {
            //Check paramaters
            int port = 0;
            int.TryParse(Port, out port);

            if (string.IsNullOrWhiteSpace(Ip))
            {
                ErrorMessage = "IP Cannot be empty.";
                return;
            }

            if(port < 1025 || port > 65535)
            {
                ErrorMessage = "Port cannot be lower than 1025 or higher than 65535";
                return;
            }

            if(string.IsNullOrWhiteSpace(Key) || Key.Length < 5)
            {
                ErrorMessage = "Key cannot be empty or lower than 5 characters";
                return;
            }

            //Permissions Check
            var status = PermissionStatus.Unknown;

            status = await Permissions.CheckStatusAsync<Permissions.Microphone>();

            if (status != PermissionStatus.Granted)
            {
                if (Permissions.ShouldShowRationale<Permissions.Microphone>())
                {
                    await Shell.Current.DisplayAlert("Needs Permission", "VoiceCraft requires microphone access in order to work and communicate with other people!", "OK");
                }

                status = await Permissions.RequestAsync<Permissions.Microphone>();
            }

            if (status != PermissionStatus.Granted)
            {
                await Shell.Current.DisplayAlert("Needs Permission", "Could not login as microphone access was denied", "OK");
                return;
            }

            //Connection Flow
            try
            {
                if (ConnectBtnMessage != "Connecting...")
                {
                    ErrorMessage = "";
                    ConnectBtnMessage = "Connecting...";
                    _ = Task.Run(() =>
                    {
                        UdpNetwork.Instance.Connect(IPAddress.Parse(Ip), Convert.ToInt16(Port), Key);
                        UdpNetwork.Instance.Authenticate(OnAuthenticate);
                    });
                }
            }
            catch (Exception ex)
            {
                ConnectBtnMessage = "Connect";
                ErrorMessage = ex.Message;
            }
        }

        async void OnAuthenticate(bool Authenticated)
        {
            ConnectBtnMessage = "Connect";
            if (Authenticated)
            {
                await Shell.Current.GoToAsync($"///./{nameof(MainPage)}");
            }
            else
            {
                UdpNetworkHandler.Instance.Disconnect();
                ErrorMessage = "Could not authenticate. Server either denied key or does not exist.";
            }
        }
    }
}
