using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NAudio.Wave;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using VoiceCraftProximityChat.Models;
using VoiceCraftProximityChat.Storage;
using VoiceCraftProximityChat.Views;

namespace VoiceCraftProximityChat.ViewModels
{
    public partial class CreditsPageViewModel : ObservableObject
    {
        [ObservableProperty]
        private SettingsModel settings = new SettingsModel();

        [ObservableProperty]
        private ObservableCollection<AudioDeviceModel> inputDevices = new ObservableCollection<AudioDeviceModel>();

        [ObservableProperty]
        private ObservableCollection<AudioDeviceModel> outputDevices = new ObservableCollection<AudioDeviceModel>();

        [ObservableProperty]
        private int inputDeviceIndex;

        [ObservableProperty]
        private int outputDeviceIndex;

        public CreditsPageViewModel()
        {
            Settings = Database.GetSettings();
            InputDevices.Add(new AudioDeviceModel() { DeviceIndex = -1, DeviceName = "Default" });
            OutputDevices.Add(new AudioDeviceModel() { DeviceIndex = -1, DeviceName = "Default" });

            for(int i = 0; i < WaveIn.DeviceCount; i++)
                InputDevices.Add(new AudioDeviceModel() { DeviceIndex = i, DeviceName = WaveIn.GetCapabilities(i).ProductName });

            for (int i = 0; i < WaveOut.DeviceCount; i++)
                OutputDevices.Add(new AudioDeviceModel() { DeviceIndex = i, DeviceName = WaveOut.GetCapabilities(i).ProductName });

            if (InputDevices.Count <= Settings.InputDevice + 1)
            {
                InputDeviceIndex = 0;
                Database.SaveSettings(Settings);
            }
            else
                InputDeviceIndex = Settings.InputDevice + 1;

            if (OutputDevices.Count <= Settings.OutputDevice + 1)
            {
                OutputDeviceIndex = 0;
                Database.SaveSettings(Settings);
            }
            else
                OutputDeviceIndex = Settings.OutputDevice + 1;
        }

        [RelayCommand]
        public void Back()
        {
            var navigator = (Frame)App.Current.MainWindow.FindName("Navigator");
            navigator.Navigate(new ServersPage());
        }

        [RelayCommand]
        public void SaveSettings()
        {
            try
            {
                Settings.InputDevice = InputDevices[InputDeviceIndex].DeviceIndex;
                Settings.OutputDevice = OutputDevices[OutputDeviceIndex].DeviceIndex;
                Database.SaveSettings(Settings);
                MessageBox.Show("Settings successfully saved.", "Saved", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch 
            {
                MessageBox.Show("There was an error saving!", "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
        }
    }
}
