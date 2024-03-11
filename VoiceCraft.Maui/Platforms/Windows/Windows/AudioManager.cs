using NAudio.Wave;
using VoiceCraft.Maui.Interfaces;
using VoiceCraft.Maui.Services;

namespace VoiceCraft.Maui;

public class AudioManager : IAudioManager
{
    public async Task<IWavePlayer> CreatePlayer(ISampleProvider waveProvider)
    {
        var settings = Database.Instance.Settings;
        if (settings.WebsocketPort < 1025 || settings.WebsocketPort > 65535)
        {
            settings.WebsocketPort = 8080;
            await Database.Instance.SaveSettings();
        }

        if (settings.OutputDevice > WaveOut.DeviceCount)
        {
            settings.OutputDevice = 0;
            await Database.Instance.SaveSettings();
        }

        var Player = new WaveOutEvent();
        Player.DesiredLatency = 50;
        Player.NumberOfBuffers = 3;
        Player.DeviceNumber = settings.OutputDevice - 1;
        Player.Init(waveProvider);
        return Player;
    }

    public async Task<IWaveIn> CreateRecorder(WaveFormat waveFormat, int bufferMS)
    {
        var settings = Database.Instance.Settings;
        if (settings.WebsocketPort < 1025 || settings.WebsocketPort > 65535)
        {
            settings.WebsocketPort = 8080;
            await Database.Instance.SaveSettings();
        }

        var Recorder = new WaveInEvent();
        Recorder.WaveFormat = waveFormat;
        Recorder.BufferMilliseconds = bufferMS;
        Recorder.DeviceNumber = settings.InputDevice - 1;
        return Recorder;
    }
}