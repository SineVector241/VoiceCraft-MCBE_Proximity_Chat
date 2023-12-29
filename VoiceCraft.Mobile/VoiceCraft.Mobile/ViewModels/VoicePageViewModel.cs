using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Linq;
using VoiceCraft.Core.Client;
using VoiceCraft.Mobile.Models;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace VoiceCraft.Mobile.ViewModels
{
    public partial class VoicePageViewModel : ObservableObject
    {
        [ObservableProperty]
        string statusText = "Connecting...";

        [ObservableProperty]
        string passwordInput = string.Empty;

        [ObservableProperty]
        bool isMuted = false;

        [ObservableProperty]
        bool isDeafened = false;

        [ObservableProperty]
        bool isSpeaking = false;

        [ObservableProperty]
        bool showSlider = false;

        [ObservableProperty]
        bool showChannels = false;

        [ObservableProperty]
        bool showPasswordInput = false;

        [ObservableProperty]
        VoiceCraftParticipant? selectedParticipant;

        [ObservableProperty]
        VoiceCraftChannel? selectedChannel;

        [ObservableProperty]
        ObservableCollection<ParticipantDisplayModel> participants = new ObservableCollection<ParticipantDisplayModel>();

        [ObservableProperty]
        ObservableCollection<ChannelDisplayModel> channels = new ObservableCollection<ChannelDisplayModel>();

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
        }

        [RelayCommand]
        public void OnDisappearing()
        {
        }

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

        [RelayCommand]
        public void ShowParticipantVolume(VoiceCraftParticipant participant)
        {
            SelectedParticipant = participant;
            ShowSlider = true;
        }

        [RelayCommand]
        public void HideParticipantVolume()
        {
            ShowSlider = false;
        }
    }
}
