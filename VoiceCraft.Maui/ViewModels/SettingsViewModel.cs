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
#if ANDROID
                var status = await Permissions.RequestAsync<Permissions.Microphone>();
                if (Permissions.ShouldShowRationale<Permissions.Microphone>())
                {
                    await Shell.Current.DisplayAlert("Error", "VoiceCraft requires the microphone to communicate with other users!", "OK");
                    return;
                }
                if (status != PermissionStatus.Granted) return;
#endif

                var manager = new AudioManager();
                Microphone = manager.CreateRecorder(AudioFormat, 20);
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
