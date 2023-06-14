using NAudio.Wave;
using VoiceCraft.Mobile.Droid.Audio;
using VoiceCraft.Mobile.Interfaces;

[assembly: Xamarin.Forms.Dependency(typeof(AudioManager))]
namespace VoiceCraft.Mobile.Droid.Audio
{
    public class AudioManager : IAudioManager
    {
        public IWavePlayer CreatePlayer(ISampleProvider waveProvider)
        {
            var Player = new AudioTrackOut();
            Player.Init(waveProvider);
            Player.DesiredLatency = 400;
            Player.NumberOfBuffers = 3;
            return Player;
        }

        public IWaveIn CreateRecorder(WaveFormat waveFormat)
        {
            var Recorder = new AudioRecorder();
            Recorder.WaveFormat = waveFormat;
            Recorder.BufferMilliseconds = 40;
            Recorder.audioSource = Android.Media.AudioSource.VoiceCommunication;
            return Recorder;
        }
    }
}