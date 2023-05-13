using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using VoiceCraft_Android.Models;
using VoiceCraft_Android.Storage;

namespace VoiceCraft_Android.ViewModels
{
    public partial class CreditsAndInfoPageViewModel : ObservableObject
    {
        [ObservableProperty]
        private SettingsModel settings = new SettingsModel();

        public CreditsAndInfoPageViewModel()
        {
            Settings = Database.GetSettings();
        }

        [RelayCommand]
        public async void SaveSettings()
        {
            try
            {
                Database.SaveSettings(Settings);
                await Utils.DisplayAlert("Saved", "Settings successfully saved");
            }
            catch
            {
                await Utils.DisplayAlert("Error", "An error has occured while saving settings...");
            }
        }
    }
}
