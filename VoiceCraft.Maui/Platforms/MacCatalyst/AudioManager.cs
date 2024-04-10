using NAudio.Wave;
using VoiceCraft.Maui.Interfaces;

namespace VoiceCraft.Maui
{
    public class AudioManager : IAudioManager
    {
        public static AudioManager Instance { get; } = new AudioManager();

        public IWavePlayer CreatePlayer(ISampleProvider AudioFormat)
        {
            throw new NotImplementedException();
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
