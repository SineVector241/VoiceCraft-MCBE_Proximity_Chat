using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace VoiceCraft_Android.ViewModels
{
    public partial class VoicePageViewModel : ObservableObject
    {
        private bool IsDeafened = false;
        private bool IsMuted = false;

        [ObservableProperty]
        string statusText = "Connecting...";

        [RelayCommand]
        public void MuteUnmute()
        {
        }

        [RelayCommand]
        public void DeafenUndeafen()
        {
        }

        [RelayCommand]
        public void Disconnect() 
        { 
        }

        public VoicePageViewModel()
        {
            App.Current.PageAppearing += PageAppearing;
        }

        private void PageAppearing(object sender, Xamarin.Forms.Page e)
        {
            throw new System.NotImplementedException();
        }
    }
}
