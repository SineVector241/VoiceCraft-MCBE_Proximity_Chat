using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NAudio.Wave;
using System.Collections.ObjectModel;
using VoiceCraft.Windows.Models;
using VoiceCraft.Windows.Storage;
using System;
using System.Windows;

namespace VoiceCraft.Windows.ViewModels
{
    public partial class SettingsPageViewModel : ObservableObject
    {
        [ObservableProperty]
        public SettingsModel settings = Database.GetSettings();

        [ObservableProperty]
        private ObservableCollection<string> inputDevices = new ObservableCollection<string>();

        [ObservableProperty]
        private ObservableCollection<string> outputDevices = new ObservableCollection<string>();

        public SettingsPageViewModel()
        {
            InputDevices.Add("Default");
            OutputDevices.Add("Default");

            for (int i = 0; i < WaveIn.DeviceCount; i++)
                InputDevices.Add(WaveIn.GetCapabilities(i).ProductName);

            for (int i = 0; i < WaveOut.DeviceCount; i++)
                OutputDevices.Add(WaveOut.GetCapabilities(i).ProductName);

            if (Settings.WebsocketPort < 1025 || Settings.WebsocketPort > 65535)
            {
                Settings.WebsocketPort = 8080;
                Database.SetSettings(Settings);
            }

            if(Settings.InputDevice > WaveIn.DeviceCount)
            {
                Settings.InputDevice = 0;
                Database.SetSettings(Settings);
            }

            if (Settings.OutputDevice > WaveOut.DeviceCount)
            {
                Settings.OutputDevice = 0;
                Database.SetSettings(Settings);
            }
        }

        [RelayCommand]
        public void GoBack()
        {
            Navigator.GoToPreviousPage();
        }

        [RelayCommand]
        public void Save()
        {
            try
            {
                Settings.SoftLimiterGain = (float)Math.Round(Settings.SoftLimiterGain, 2);
                Database.SetSettings(Settings);
                MessageBox.Show("Successfully saved settings.", "Saved", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch(Exception ex)
            {
                MessageBox.Show($"An error occured!\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        [RelayCommand]
        public void Reset()
        {
            try
            {
                Settings = new SettingsModel();
                Database.SetSettings(Settings);
                MessageBox.Show("Successfully reset settings.", "Reset", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occured!\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
