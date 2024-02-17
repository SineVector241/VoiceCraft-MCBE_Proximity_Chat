using NAudio.Wave;
using VoiceCraft.Core.Client;
using VoiceCraft.Mobile.Droid.Audio;
using VoiceCraft.Mobile.Interfaces;
using VoiceCraft.Mobile.Storage;

[assembly: Xamarin.Forms.Dependency(typeof(AudioManager))]
namespace VoiceCraft.Mobile.Droid.Audio
{
    public class AudioManager : IAudioManager
    {
        public IWavePlayer CreatePlayer(ISampleProvider waveProvider)
        {
            var settings = Database.GetSettings();
            if (settings.WebsocketPort < 1025 || settings.WebsocketPort > 65535)
            {
                settings.WebsocketPort = 8080;
                Database.SetSettings(settings);
            }

            var Player = new AudioTrackOut();
            Player.Init(waveProvider);
            Player.DesiredLatency = 400;
            Player.NumberOfBuffers = 3;
            return Player;
        }

        public IWaveIn CreateRecorder(WaveFormat waveFormat)
        {
            var settings = Database.GetSettings();
            if (settings.WebsocketPort < 1025 || settings.WebsocketPort > 65535)
            {
                settings.WebsocketPort = 8080;
                Database.SetSettings(settings);
            }

            var Recorder = new AudioRecorder();
            Recorder.WaveFormat = waveFormat;
            Recorder.BufferMilliseconds = VoiceCraftClient.FrameMilliseconds;
            Recorder.audioSource = Android.Media.AudioSource.VoiceCommunication;
            return Recorder;
        }
    }
}