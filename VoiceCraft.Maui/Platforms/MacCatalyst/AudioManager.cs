using NAudio.Wave;
using VoiceCraft.Maui.Interfaces;

namespace VoiceCraft.Maui
{
    public class AudioManager : IAudioManager
    {
        public IWavePlayer CreatePlayer(ISampleProvider AudioFormat)
        {
            throw new NotImplementedException();
        }

        public IWaveIn CreateRecorder(WaveFormat AudioFormat, int bufferMS)
        {
            throw new NotImplementedException();
        }
    }
}
