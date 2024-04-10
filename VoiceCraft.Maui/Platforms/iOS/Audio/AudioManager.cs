using NAudio.Wave;
using VoiceCraft.Maui.Interfaces;

namespace VoiceCraft.Maui
{
    public class AudioManager : IAudioManager
    {
        public static AudioManager Instance { get; } = new AudioManager();

        public IWavePlayer CreatePlayer(ISampleProvider waveProvider)
        {
            var Player = new AVAudioEngineOut();
            Player.DesiredLatency = 50;
            Player.NumberOfBuffers = 3;
            Player.Init(waveProvider);
            return Player;
        }

        public IWaveIn CreateRecorder(WaveFormat AudioFormat, int bufferMS)
        {
            throw new NotImplementedException();
        }

        public string[] GetInputDevices()
        {
            throw new NotImplementedException();
        }

        public string[] GetOutputDevices()
        {
            throw new NotImplementedException();
        }

        public int GetInputDeviceCount()
        {
            throw new NotImplementedException();
        }

        public int GetOutputDeviceCount()
        {
            throw new NotImplementedException();
        }

        public bool RequestInputPermissions()
        {
            throw new NotImplementedException();
        }

        public bool RequestOutputPermissions()
        {
            throw new NotImplementedException();
        }
    }
}
