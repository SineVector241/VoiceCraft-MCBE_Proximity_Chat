using NAudio.Wave;
using VoiceCraftProximityChat.Interfaces;

namespace VoiceCraftProximityChat.Audio
{
    public class AudioManager : IAudioManager
    {
        public IWavePlayer CreatePlayer(ISampleProvider waveFormat, int deviceIndex)
        {
            var Player = new WaveOutEvent();
            Player.Init(waveFormat);
            Player.DesiredLatency = 100;
            Player.DeviceNumber = deviceIndex;
            return Player;
        }

        public IWaveIn CreateRecorder(WaveFormat waveFormat, int deviceIndex)
        {
            var Recorder = new WaveInEvent();
            Recorder.WaveFormat = waveFormat;
            Recorder.BufferMilliseconds = 40;
            Recorder.DeviceNumber = deviceIndex;
            return Recorder;
        }
    }
}
