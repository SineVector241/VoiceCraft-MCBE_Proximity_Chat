using NAudio.Wave;
using VoiceCraft.Maui.Interfaces;
namespace VoiceCraft.Maui
{
    public class AudioManager : IAudioManager
    {
        public IWavePlayer CreatePlayer(ISampleProvider waveProvider)
        {
            var Player = new AVAudioEngineOut();
            Player.DesiredLatency = 50;
            Player.NumberOfBuffers = 3;
            Player.Init(waveProvider);
            return Player;
        }

        public IWaveIn CreateRecorder(WaveFormat waveFormat, int bufferMS)
        {
            var Recorder = new AVAudioEngineIn();
            Recorder.WaveFormat = waveFormat;
            Recorder.BufferMilliseconds = bufferMS;
            return Recorder;
        }
    }
}
