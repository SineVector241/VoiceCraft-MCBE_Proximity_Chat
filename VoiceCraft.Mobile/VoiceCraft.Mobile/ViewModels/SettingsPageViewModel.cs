using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NAudio.Wave;
using System;
using VoiceCraft.Core.Client;
using VoiceCraft.Mobile.Interfaces;
using VoiceCraft.Mobile.Models;
using VoiceCraft.Mobile.Storage;
using Xamarin.Forms;

namespace VoiceCraft.Mobile.ViewModels
{
    public partial class SettingsPageViewModel : ObservableObject
    {
        [ObservableProperty]
        public SettingsModel settings = Database.GetSettings();

        [ObservableProperty]
        private float micDetection = 0.0f;

        [ObservableProperty]
        private bool micOpen = false;

        private IWaveIn AudioRecorder;

        public SettingsPageViewModel()
        {
            var audioManager = DependencyService.Get<IAudioManager>();
            AudioRecorder = audioManager.CreateRecorder(new WaveFormat(VoiceCraftClient.SampleRate, 1));
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
        public void SaveSettings()
        {
            try
            {
                Settings.SoftLimiterGain = (float)Math.Round(Settings.SoftLimiterGain, 2);
                Database.SetSettings(Settings);
                Shell.Current.DisplayAlert("Save", "Successfully saved settings!", "OK");
            }
            catch (Exception ex)
            {
                Shell.Current.DisplayAlert("Error", $"An error occured!\n{ex.Message}", "OK");
            }
        }

        [RelayCommand]
        public async void OpenCloseMicrophone()
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
                    var granted = await Utils.CheckAndRequestPermissions();
                    if (granted)
                    {
                        AudioRecorder.StartRecording();
                        MicOpen = true;
                    }
                }
            }
            catch (Exception ex)
            {
                _ = Shell.Current.DisplayAlert("Error", $"An error occured!\n{ex.Message}", "OK");
            }
        }

        [RelayCommand]
        public void ResetSettings()
        {
            try
            {
                Settings = new SettingsModel();
                Database.SetSettings(Settings);
                Shell.Current.DisplayAlert("Reset", "Successfully reset settings!", "OK");
            }
            catch (Exception ex)
            {
                Shell.Current.DisplayAlert("Error", $"An error occured!\n{ex.Message}", "OK");
            }
        }

        [RelayCommand]
        public void OnDisappearing()
        {
            if (MicOpen)
                AudioRecorder.StopRecording();
            AudioRecorder.DataAvailable -= AudioDataAvailable;
            AudioRecorder.RecordingStopped -= RecorderStopped;
            Settings = Database.GetSettings();
            MicDetection = 0;
        }

        [RelayCommand]
        public void OnAppearing()
        {
            AudioRecorder.DataAvailable += AudioDataAvailable;
            AudioRecorder.RecordingStopped += RecorderStopped;
        }
    }
}
