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
        bool isDeafened = false;

        [ObservableProperty]
        string statusText = "Connecting...";

        [ObservableProperty]
        bool isSpeaking = false;

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
        public void DeafenUndeafen()
        {
            var message = new DeafenUndeafen();
            MessagingCenter.Send(message, "DeafenUndeafen");
            IsDeafened = !IsDeafened;
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

                if(IsDeafened != message.IsDeafened)
                    IsDeafened = message.IsDeafened;

                if (IsSpeaking != message.IsSpeaking)
                    IsSpeaking = message.IsSpeaking;

                Participants = new ObservableCollection<string>(message.Participants);
            });

            MessagingCenter.Subscribe<DisconnectMessage>(this, "Disconnected", message =>
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    if (!string.IsNullOrWhiteSpace(message.Reason))
                        Shell.Current.DisplayAlert("Disconnected!", message.Reason, "OK");
                });
            });
        }

        [RelayCommand]
        public void OnDisappearing()
        {
            MessagingCenter.Unsubscribe<StopServiceMessage>(this, "ServiceStopped");
            MessagingCenter.Unsubscribe<UpdateUIMessage>(this, "Update");
            MessagingCenter.Unsubscribe<DisconnectMessage>(this, "Disconnected");
        }
    }
}
