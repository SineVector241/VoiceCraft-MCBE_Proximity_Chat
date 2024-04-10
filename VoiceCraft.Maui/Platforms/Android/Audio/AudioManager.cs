using NAudio.Wave;
using VoiceCraft.Maui.Interfaces;

namespace VoiceCraft.Maui;

public class AudioManager : IAudioManager
{
    public static AudioManager Instance { get; } = new AudioManager();

    public IWavePlayer CreatePlayer(ISampleProvider waveProvider)
    {
        var Player = new AudioTrackOut();
        Player.DesiredLatency = 50;
        Player.NumberOfBuffers = 3;
        Player.Init(waveProvider);
        return Player;
    }

    public IWaveIn CreateRecorder(WaveFormat waveFormat, int bufferMS)
    {
        var Recorder = new AudioRecorder();
        Recorder.WaveFormat = waveFormat;
        Recorder.BufferMilliseconds = bufferMS;
        Recorder.audioSource = Android.Media.AudioSource.VoiceCommunication;
        return Recorder;
    }

    public string[] GetInputDevices()
    {
        return new string[0];
    }

    public string[] GetOutputDevices()
    {
        var outputDevices = new List<string>()
        {
            "Phone",
            "Speaker"
        };
        return outputDevices.ToArray();
    }

    public int GetInputDeviceCount()
    {
        return 0;
    }

    public int GetOutputDeviceCount()
    {
        return 2;
    }

    public bool RequestInputPermissions()
    {
        var status = Permissions.RequestAsync<Permissions.Microphone>().GetAwaiter().GetResult();
        if (Permissions.ShouldShowRationale<Permissions.Microphone>())
        {
            Shell.Current.DisplayAlert("Error", "VoiceCraft requires the microphone to communicate with other users!", "OK").Wait();
            return false;
        }
        
        return status != PermissionStatus.Granted;
    }

    public bool RequestOutputPermissions()
    {
        return true;
    }
}