using NAudio.Wave;
using VoiceCraft.Maui.Interfaces;
using VoiceCraft.Maui.Services;

namespace VoiceCraft.Maui;

public class AudioManager : IAudioManager
{
    public IWavePlayer CreatePlayer(ISampleProvider waveProvider)
    {
        var settings = Database.Instance.Settings;

        var Player = new WaveOutEvent();
        Player.DesiredLatency = 50;
        Player.NumberOfBuffers = 3;
        Player.DeviceNumber = settings.OutputDevice - 1;
        Player.Init(waveProvider);
        return Player;
    }

    public IWaveIn CreateRecorder(WaveFormat waveFormat, int bufferMS)
    {
        var settings = Database.Instance.Settings;

        var Recorder = new WaveInEvent();
        Recorder.WaveFormat = waveFormat;
        Recorder.BufferMilliseconds = bufferMS;
        Recorder.DeviceNumber = settings.InputDevice - 1;
        return Recorder;
    }
}