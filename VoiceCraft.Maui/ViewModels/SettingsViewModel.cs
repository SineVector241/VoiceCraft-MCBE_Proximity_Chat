using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NAudio.Wave;
using System.Collections.ObjectModel;
using VoiceCraft.Maui.Services;
using VoiceCraft.Models;

namespace VoiceCraft.Maui.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        [ObservableProperty]
        SettingsModel settings = Database.Instance.Settings;

        [ObservableProperty]
        ObservableCollection<string> inputDevices = new ObservableCollection<string>() { "Default" };
        
        [ObservableProperty]
        ObservableCollection<string> outputDevices = new ObservableCollection<string>() { "Default" };

        public SettingsViewModel()
        {
#if WINDOWS
            for (int i = 0; i < WaveIn.DeviceCount; i++)
                inputDevices.Add(WaveIn.GetCapabilities(i).ProductName);

            for (int i = 0; i < WaveOut.DeviceCount; i++)
                outputDevices.Add(WaveOut.GetCapabilities(i).ProductName);

            if (settings.InputDevice > WaveIn.DeviceCount)
            {
                Settings.InputDevice = 0;
            }
            if (settings.OutputDevice > WaveOut.DeviceCount)
            {
                Settings.OutputDevice = 0;
            }
#endif
        }

        [RelayCommand]
        public void SaveSettings()
        {
            _ = Database.Instance.SaveSettings();
        }
    }
}
