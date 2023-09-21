using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NAudio.Wave;
using System.Collections.ObjectModel;
using VoiceCraft.Windows.Models;
using VoiceCraft.Windows.Storage;
using System;
using System.Windows;
using VoiceCraft.Windows.Audio;
using VoiceCraft.Core.Client;

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

        [ObservableProperty]
        private float micDetection = 0.0f;

        [ObservableProperty]
        private bool micOpen = false;

        private IWaveIn AudioRecorder;
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

            var audioManager = new AudioManager();
            AudioRecorder = audioManager.CreateRecorder(new WaveFormat(VoiceCraftClient.SampleRate, 1));
            AudioRecorder.DataAvailable += AudioDataAvailable;
            AudioRecorder.RecordingStopped += RecorderStopped;
        }

        private void RecorderStopped(object? sender, StoppedEventArgs e)
        {
            MicOpen = false;
        }

        private void AudioDataAvailable(object? sender, WaveInEventArgs e)
        {
            float max = 0;
            // interpret as 16 bit audio
            for (int index = 0; index < e.BytesRecorded; index += 2)
            {
                short sample = (short)((e.Buffer[index + 1] << 8) |
                                        e.Buffer[index + 0]);
                // to floating point
                var sample32 = sample / 32768f;
                // absolute value 
                if (sample32 < 0) sample32 = -sample32;
                if (sample32 > max) max = sample32;
            }
            MicDetection = max;
        }

        [RelayCommand]
        public void GoBack()
        {
            if(MicOpen)
                AudioRecorder.StopRecording();
            AudioRecorder.DataAvailable -= AudioDataAvailable;
            AudioRecorder.RecordingStopped -= RecorderStopped;
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
        public void OpenCloseMicrophone()
        {
            try 
            {
                if (MicOpen)
                {
                    AudioRecorder.StopRecording();
                    MicOpen = false;
                    MicDetection = 0;
                }
                else
                {
                    AudioRecorder.StartRecording();
                    MicOpen = true;
                }
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
