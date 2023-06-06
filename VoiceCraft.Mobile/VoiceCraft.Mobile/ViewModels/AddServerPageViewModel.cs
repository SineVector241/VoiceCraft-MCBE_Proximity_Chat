using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using VoiceCraft.Mobile.Models;
using VoiceCraft.Mobile.Storage;
using Xamarin.Forms;

namespace VoiceCraft.Mobile.ViewModels
{
    public partial class AddServerPageViewModel : ObservableObject
    {
        [ObservableProperty]
        ServerModel server = new ServerModel();

        [ObservableProperty]
        ObservableCollection<string> codecs = new ObservableCollection<string>() { "Opus - 3.75kb/s (48khz)", "G722 - 16kb/s (16khz)" };

        [RelayCommand]
        public void AddServer()
        {
            try
            {
                Database.AddServer(Server);
                Shell.Current.Navigation.PopAsync();
            }
            catch(Exception ex)
            {
                Shell.Current.DisplayAlert("Error!", ex.Message, "OK");
            }
        }

        [RelayCommand]
        public async void Cancel()
        {
            await Shell.Current.Navigation.PopAsync();
        }
    }
}
