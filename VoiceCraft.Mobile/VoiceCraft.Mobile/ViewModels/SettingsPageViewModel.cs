using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
            Database.SetSettings(Settings);
            Shell.Current.DisplayAlert("Save", "Successfully saved settings!", "OK");
        }
    }
}
