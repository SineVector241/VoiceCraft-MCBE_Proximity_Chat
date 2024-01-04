using NAudio.Wave;
using VoiceCraft.Windows.Interfaces;
using VoiceCraft.Windows.Storage;

namespace VoiceCraft.Windows.Audio
{
    public class AudioManager : IAudioManager
    {
        public IWavePlayer CreatePlayer(ISampleProvider waveProvider)
        {
            var settings = Database.GetSettings();
            if (settings.OutputDevice > WaveOut.DeviceCount)
            {
                settings.OutputDevice = 0;
                Database.SetSettings(settings);
            }

            if (settings.WebsocketPort < 1025 || settings.WebsocketPort > 65535)
            {
                settings.WebsocketPort = 8080;
                Database.SetSettings(settings);
            }

            var Player = new WaveOutEvent();
            Player.Init(waveProvider);
            Player.DesiredLatency = 40;
            Player.NumberOfBuffers = 3;
            Player.DeviceNumber = settings.OutputDevice - 1;
            return Player;
        }

        public IWaveIn CreateRecorder(WaveFormat waveFormat)
        {
            var settings = Database.GetSettings();
            if(settings.InputDevice > WaveIn.DeviceCount)
            {
                settings.InputDevice = 0;
                Database.SetSettings(settings);
            }

            if (settings.WebsocketPort < 1025 || settings.WebsocketPort > 65535)
            {
                settings.WebsocketPort = 8080;
                Database.SetSettings(settings);
            }

            var Recorder = new WaveInEvent();
            Recorder.WaveFormat = waveFormat;
            Recorder.BufferMilliseconds = 40;
            Recorder.DeviceNumber = settings.InputDevice - 1;
            return Recorder;
        }
    }
}