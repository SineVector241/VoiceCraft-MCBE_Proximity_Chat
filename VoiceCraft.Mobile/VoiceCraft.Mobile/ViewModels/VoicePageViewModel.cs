using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using VoiceCraft.Mobile.Services;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace VoiceCraft.Mobile.ViewModels
{
    public partial class VoicePageViewModel : ObservableObject
    {
        [ObservableProperty]
        bool isMuted = false;

        [ObservableProperty]
        string statusText = "Connecting...";

        [ObservableProperty]
        ObservableCollection<string> participants = new ObservableCollection<string>();

        [RelayCommand]
        public void MuteUnmute()
        {
            var message = new MuteUnmuteMessage();
            MessagingCenter.Send(message, "MuteUnmute");
            IsMuted = !IsMuted;
        }

        [RelayCommand]
        public void Disconnect()
        {
            MessagingCenter.Send(new DisconnectMessage(), "Disconnect");
        }

        //Page codebehind to viewmodel.
        [RelayCommand]
        public void OnAppearing()
        {
            if (Preferences.Get("VoipServiceRunning", false) == false)
            {
                Device.BeginInvokeOnMainThread(() => {
                    Shell.Current.Navigation.PopAsync();
                });
                return;
            }

            MessagingCenter.Subscribe<StopServiceMessage>(this, "ServiceStopped", message =>
            {
                Device.BeginInvokeOnMainThread(() => {
                    Shell.Current.Navigation.PopAsync();
                });
            });

            MessagingCenter.Subscribe<UpdateUIMessage>(this, "Update", message =>
            {
                if (StatusText != message.StatusMessage)
                    StatusText = message.StatusMessage;

                if (IsMuted != message.IsMuted)
                    IsMuted = message.IsMuted;
            });

            MessagingCenter.Subscribe<ServiceFailedMessage>(this, "Error", message =>
            {
                Device.BeginInvokeOnMainThread(() => {
                    Shell.Current.DisplayAlert("Error", message.Message, "OK");
                });
            });
        }

        [RelayCommand]
        public void OnDisappearing()
        {
            MessagingCenter.Unsubscribe<StopServiceMessage>(this, "ServiceStopped");
            MessagingCenter.Unsubscribe<UpdateUIMessage>(this, "Update");
            MessagingCenter.Unsubscribe<ServiceFailedMessage>(this, "Error");
        }
    }
}
