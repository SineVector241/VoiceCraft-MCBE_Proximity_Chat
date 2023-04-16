using NAudio.Wave;

namespace VoiceCraft_Android.Interfaces
{
    public interface IAudioManager
    {
        IWaveIn CreateRecorder(WaveFormat waveFormat);
        IWavePlayer CreatePlayer(ISampleProvider waveFormat);
    }
}
