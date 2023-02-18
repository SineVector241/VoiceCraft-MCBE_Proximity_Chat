using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Linq;

namespace VoiceCraft_Mobile.ViewModels
{
    public partial class VoicePageViewModel : BaseViewModel
    {
        [ObservableProperty]
        private string statusMessage = "Connecting...";

        [RelayCommand]
        async void Disconnect()
        {
            await App.Current.MainPage.Navigation.NavigationStack.LastOrDefault().Navigation.PopAsync();
        }
    }
}
