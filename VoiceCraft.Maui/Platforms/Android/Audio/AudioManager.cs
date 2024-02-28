using NAudio.Wave;
using VoiceCraft.Core;
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

        var Player = new AudioTrackOut();
        Player.DesiredLatency = 400;
        Player.NumberOfBuffers = 3;
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

        var Recorder = new AudioRecorder();
        Recorder.WaveFormat = waveFormat;
        Recorder.BufferMilliseconds = bufferMS;
        Recorder.audioSource = Android.Media.AudioSource.VoiceCommunication;
        return Recorder;
    }
}