using NAudio.Wave.SampleProviders;
using NAudio.Wave;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace VoiceCraftProximityChat.Models
{
    class AudioPlaybackModel
    {
        private readonly IWavePlayer outputDevice;
        private readonly MixingSampleProvider mixer;
        private readonly BufferedWaveProvider waveProvider;

        public AudioPlaybackModel(int sampleRate = 44100, int channelCount = 2)
        {
            outputDevice = new WaveOutEvent();
            mixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(16000, 1));
            waveProvider = new BufferedWaveProvider(WaveFormat.CreateIeeeFloatWaveFormat(16000, 1));
            waveProvider.DiscardOnBufferOverflow = false;
            waveProvider.ReadFully = false;
            mixer.ReadFully = true;
            outputDevice.Init(mixer);
            outputDevice.Play();
        }

        public void PlaySound(byte[] buffer, float Volume)
        {
            Task.Run(() =>
            {
                var provider = new RawSourceWaveStream(buffer, 0, 3200, WaveFormat.CreateIeeeFloatWaveFormat(16000, 1));
                waveProvider.AddSamples(buffer, 0, 3200);
                mixer.AddMixerInput(waveProvider);
            });
        }

        public static readonly AudioPlaybackModel Instance = new AudioPlaybackModel(16000, 1);
    }
}
