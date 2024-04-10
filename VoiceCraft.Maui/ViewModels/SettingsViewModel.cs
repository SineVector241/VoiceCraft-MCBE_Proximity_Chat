using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using VoiceCraft.Maui.Services;
using VoiceCraft.Maui.Models;
using NAudio.Wave;

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

        [ObservableProperty]
        float microphoneDetection;

        [ObservableProperty]
        bool isRecording = false;

        private IWaveIn? Microphone;
        private WaveFormat AudioFormat = new WaveFormat(48000, 1);

        public SettingsViewModel()
        {
            foreach(var device in AudioManager.Instance.GetInputDevices())
                InputDevices.Add(device);

            foreach (var device in AudioManager.Instance.GetOutputDevices())
                OutputDevices.Add(device);

            if (Settings.InputDevice > AudioManager.Instance.GetInputDeviceCount())
            {
                Settings.InputDevice = 0;
            }
            if (Settings.OutputDevice > AudioManager.Instance.GetOutputDeviceCount())
            {
                Settings.OutputDevice = 0;
            }
        }

        [RelayCommand]
        public void SaveSettings()
        {
            if(Microphone != null)
            {
                Microphone.StopRecording();
                Microphone.DataAvailable -= Microphone_DataAvailable;
                Microphone.Dispose();
                Microphone = null;
                MicrophoneDetection = 0;
                IsRecording = false;
            }
            _ = Database.Instance.SaveSettings();
        }

        [RelayCommand]
        public void OpenCloseMicrophone()
        {
            if (Microphone == null)
            {
                if (!AudioManager.Instance.RequestInputPermissions()) return;
                Microphone = AudioManager.Instance.CreateRecorder(AudioFormat, 20);
                Microphone.DataAvailable += Microphone_DataAvailable;
                Microphone.StartRecording();
                IsRecording = true;
            }
            else
            {
                Microphone.StopRecording();
                Microphone.DataAvailable -= Microphone_DataAvailable;
                Microphone.Dispose();
                Microphone = null;
                MicrophoneDetection = 0;
                IsRecording = false;
            }
        }

        private void Microphone_DataAvailable(object? sender, WaveInEventArgs e)
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
            MicrophoneDetection = max;
        }
    }
}
