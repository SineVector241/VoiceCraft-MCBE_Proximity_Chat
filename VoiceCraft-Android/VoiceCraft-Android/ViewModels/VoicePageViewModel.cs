using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using VoiceCraft_Android.Services;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace VoiceCraft_Android.ViewModels
{
    public partial class VoicePageViewModel : ObservableObject
    {
        private bool IsDeafened = false;
        private bool IsMuted = false;

        [ObservableProperty]
        string statusText = "Connecting...";

        [ObservableProperty]
        ObservableCollection<string> participants = new ObservableCollection<string>();

        [RelayCommand]
        public void MuteUnmute()
        {
            var message = new MuteUnmuteMessage();
            MessagingCenter.Send(message, "MuteUnmute");
        }

        [RelayCommand]
        public void DeafenUndeafen()
        {
        }

        [RelayCommand]
        public void Disconnect() 
        {
            StopService();
        }

        //Page codebehind to viewmodel.
        [RelayCommand]
        public void OnAppearing()
        {
            if (Preferences.Get("VoipServiceRunning", false) == false)
            {
                Utils.GoToPreviousPage();
                return;
            }

            MessagingCenter.Subscribe<StopServiceMessage>(this, "ServiceStopped", message => {
                Utils.GoToPreviousPage();
            });

            MessagingCenter.Subscribe<UpdateUIMessage>(this, "Update", message =>
            {
                    if (StatusText != message.StatusMessage)
                        StatusText = message.StatusMessage;

                    var list = new ObservableCollection<string>();
                    foreach (var part in message.Participants)
                    {
                        list.Add(part.Name);
                    }
                    if (list != Participants)
                    {
                        Participants = list;
                    }
            });
        }

        [RelayCommand]
        public void OnDisappearing()
        {
            MessagingCenter.Unsubscribe<StopServiceMessage>(this, "ServiceStopped");
            MessagingCenter.Unsubscribe<UpdateUIMessage>(this, "Update");
        }

        private void StopService()
        {
            var message = new StopServiceMessage();
            MessagingCenter.Send(message, "ServiceStopped");
            Preferences.Set("VoipServiceRunning", false);
        }
    }
}
