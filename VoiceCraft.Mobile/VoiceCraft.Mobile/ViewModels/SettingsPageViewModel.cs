using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using VoiceCraft.Mobile.Models;
using VoiceCraft.Mobile.Storage;
using Xamarin.Forms;

namespace VoiceCraft.Mobile.ViewModels
{
    public partial class SettingsPageViewModel : ObservableObject
    {
        [ObservableProperty]
        public SettingsModel settings = Database.GetSettings();

        [RelayCommand]
        public void SaveSettings()
        {
            try
            {
                Settings.SoftLimiterGain = (float)Math.Round(Settings.SoftLimiterGain, 2);
                Database.SetSettings(Settings);
                Shell.Current.DisplayAlert("Save", "Successfully saved settings!", "OK");
            }
            catch(Exception ex)
            {
                Shell.Current.DisplayAlert("Error", $"An error occured!\n{ex.Message}", "OK");
            }
        }
    }
}
