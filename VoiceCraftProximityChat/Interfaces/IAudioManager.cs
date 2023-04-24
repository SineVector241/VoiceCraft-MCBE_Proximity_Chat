using NAudio.Wave;

namespace VoiceCraftProximityChat.Interfaces
{
    public interface IAudioManager
    {
        IWaveIn CreateRecorder(WaveFormat waveFormat);
        IWavePlayer CreatePlayer(ISampleProvider waveFormat);
    }
}
