using NAudio.Wave;

namespace VoiceCraftProximityChat.Interfaces
{
    public interface IAudioManager
    {
        IWaveIn CreateRecorder(WaveFormat waveFormat, int deviceIndex);
        IWavePlayer CreatePlayer(ISampleProvider waveFormat, int deviceIndex);
    }
}
