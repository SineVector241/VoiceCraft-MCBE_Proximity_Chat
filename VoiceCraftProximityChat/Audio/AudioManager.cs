using NAudio.Wave;
using VoiceCraftProximityChat.Interfaces;

namespace VoiceCraftProximityChat.Audio
{
    public class AudioManager : IAudioManager
    {
        public IWavePlayer CreatePlayer(ISampleProvider waveFormat)
        {
            var Player = new WaveOutEvent();
            Player.Init(waveFormat);
            return Player;
        }

        public IWaveIn CreateRecorder(WaveFormat waveFormat)
        {
            var Recorder = new WaveInEvent();
            Recorder.WaveFormat = waveFormat;
            Recorder.BufferMilliseconds = 50;
            return Recorder;
        }
    }
}
