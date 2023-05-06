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
            Player.DesiredLatency = 100;
            return Player;
        }

        public IWaveIn CreateRecorder(WaveFormat waveFormat)
        {
            var Recorder = new WaveInEvent();
            Recorder.WaveFormat = waveFormat;
            Recorder.BufferMilliseconds = 20;
            return Recorder;
        }
    }
}
