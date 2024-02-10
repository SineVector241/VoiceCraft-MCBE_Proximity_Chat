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
using Gma.System.MouseKeyHook;
using NAudio.Wave.SampleProviders;

namespace VoiceCraft.Windows.ViewModels
{
    public partial class SettingsPageViewModel : ObservableObject
    {
        [ObservableProperty]
        public SettingsModel settings;

        [ObservableProperty]
        private ObservableCollection<string> inputDevices = new ObservableCollection<string>();

        [ObservableProperty]
        private ObservableCollection<string> outputDevices = new ObservableCollection<string>();

        [ObservableProperty]
        private float micDetection = 0.0f;

        [ObservableProperty]
        private bool micOpen = false;

        [ObservableProperty]
        private bool audioPlaying = false;

        [ObservableProperty]
        private bool settingMute = false;

        [ObservableProperty]
        private bool settingDeafen = false;

        private IWaveIn AudioRecorder;
        private IWavePlayer AudioPlayer;
        private ISampleProvider SineWaveOut;
        private IKeyboardMouseEvents? Events;
        public SettingsPageViewModel()
        {
            var settings = Database.GetSettings();
            //Copy the model
            this.settings = new SettingsModel()
            {
                ClientSidedPositioning = settings.ClientSidedPositioning,
                DeafenKeybind = settings.DeafenKeybind,
                DirectionalAudioEnabled = settings.DirectionalAudioEnabled,
                HideAddress = settings.HideAddress,
                InputDevice = settings.InputDevice,
                LinearVolume = settings.LinearVolume,
                MicrophoneDetectionPercentage = settings.MicrophoneDetectionPercentage,
                MuteKeybind = settings.MuteKeybind,
                OutputDevice = settings.OutputDevice,
                SoftLimiterEnabled = settings.SoftLimiterEnabled,
                SoftLimiterGain = settings.SoftLimiterGain,
                WebsocketPort = settings.WebsocketPort
            };

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

            SineWaveOut = new SignalGenerator()
            {
                Gain = 0.2,
                Frequency = 500,
                Type = SignalGeneratorType.Sin
            };

            var audioManager = new AudioManager();
            AudioRecorder = audioManager.CreateRecorder(new WaveFormat(VoiceCraftClient.SampleRate, 1));
            AudioRecorder.DataAvailable += AudioDataAvailable;
            AudioRecorder.RecordingStopped += RecorderStopped;
            
            AudioPlayer = audioManager.CreatePlayer(SineWaveOut);
            AudioPlayer.PlaybackStopped += PlayerStopped;
        }

        private void RecorderStopped(object? sender, StoppedEventArgs e)
        {
            MicOpen = false;
        }

        private void PlayerStopped(object? sender, StoppedEventArgs e)
        {
            AudioPlaying = false;
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
        public void SetMuteKeybind()
        {
            if (!SettingMute)
            {
                SettingMute = true;
                Events = Hook.GlobalEvents();
                var keys = "";
                Events.KeyDown += (object? sender, System.Windows.Forms.KeyEventArgs e) =>
                {
                    keys += string.IsNullOrWhiteSpace(keys) ? e.KeyCode : $"+{e.KeyCode}";
                    if (
                    !System.Windows.Forms.Keys.LControlKey.HasFlag(e.KeyCode) &&
                    !System.Windows.Forms.Keys.RControlKey.HasFlag(e.KeyCode) &&
                    !System.Windows.Forms.Keys.LMenu.HasFlag(e.KeyCode) &&
                    !System.Windows.Forms.Keys.RMenu.HasFlag(e.KeyCode) &&
                    !System.Windows.Forms.Keys.RShiftKey.HasFlag(e.KeyCode) &&
                    !System.Windows.Forms.Keys.LShiftKey.HasFlag(e.KeyCode) //Don't ask. IDK what I'm doing here...
                    )
                    {
                        Events.Dispose();
                        Events = null;
                        Settings.MuteKeybind = keys;
                        SettingMute = false;
                        OnPropertyChanged(nameof(Settings));
                    }
                };
            }
            else
            {
                SettingMute = false;
                Events?.Dispose();
                Events = null;
            }
        }

        [RelayCommand]
        public void SetDeafenKeybind()
        {
            if (!SettingDeafen)
            {
                SettingDeafen = true;
                Events = Hook.GlobalEvents();
                var keys = "";
                Events.KeyDown += (object? sender, System.Windows.Forms.KeyEventArgs e) =>
                {
                    keys += string.IsNullOrWhiteSpace(keys) ? e.KeyCode : $"+{e.KeyCode}";
                    if (
                    !System.Windows.Forms.Keys.LControlKey.HasFlag(e.KeyCode) &&
                    !System.Windows.Forms.Keys.RControlKey.HasFlag(e.KeyCode) &&
                    !System.Windows.Forms.Keys.LMenu.HasFlag(e.KeyCode) &&
                    !System.Windows.Forms.Keys.RMenu.HasFlag(e.KeyCode) &&
                    !System.Windows.Forms.Keys.RShiftKey.HasFlag(e.KeyCode) &&
                    !System.Windows.Forms.Keys.LShiftKey.HasFlag(e.KeyCode) //Don't ask. IDK what I'm doing here...
                    )
                    {
                        Events.Dispose();
                        Events = null;
                        Settings.DeafenKeybind = keys;
                        SettingDeafen = false;
                        OnPropertyChanged(nameof(Settings));
                    }
                };
            }
            else
            {
                SettingDeafen = false;
                Events?.Dispose();
                Events = null;
            }
        }

        [RelayCommand]
        public void GoBack()
        {
            if (Events != null)
            {
                SettingDeafen = false;
                SettingMute = false;
                Events.Dispose();
                Events = null;
            }
            if (MicOpen)
                AudioRecorder.StopRecording();
            AudioRecorder.DataAvailable -= AudioDataAvailable;
            AudioRecorder.RecordingStopped -= RecorderStopped;
            AudioPlayer.PlaybackStopped -= PlayerStopped;
            AudioPlayer.Dispose();
            AudioRecorder.Dispose();
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
        public void StartStopPlaying()
        {
            try
            {
                if(AudioPlaying)
                {
                    AudioPlayer.Stop();
                    AudioPlaying = false;
                }
                else
                {
                    AudioPlayer.Play();
                    AudioPlaying = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occured!\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        [RelayCommand]
        public void Reset()
        {
            try
            {
                if (Events != null)
                {
                    SettingDeafen = false;
                    SettingMute = false;
                    Events.Dispose();
                    Events = null;
                }
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
