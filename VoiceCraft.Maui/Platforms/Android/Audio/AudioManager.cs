using NAudio.Wave;
using VoiceCraft.Maui.Interfaces;

namespace VoiceCraft.Maui;

public class AudioManager : IAudioManager
{
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
}