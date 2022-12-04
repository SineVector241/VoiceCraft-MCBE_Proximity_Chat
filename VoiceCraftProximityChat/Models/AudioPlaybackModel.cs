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

        public AudioPlaybackModel(int sampleRate = 44100, int channelCount = 2)
        {
            outputDevice = new WaveOutEvent();
            mixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(16000, 1));
            mixer.ReadFully = true;
            outputDevice.Init(mixer);
            outputDevice.Play();
        }

        public void PlaySound(byte[] buffer, float Volume)
        {
            Task.Run(() =>
            {
                var provider = new BufferedWaveProvider(new WaveFormat(32000, 1))
                {
                    DiscardOnBufferOverflow = false,
                    ReadFully = false
                };
                provider.AddSamples(buffer, 0, 3200);
                var media = new MediaFoundationResampler(provider, WaveFormat.CreateIeeeFloatWaveFormat(16000, 1));
                mixer.AddMixerInput(media);
            });
        }

        public static readonly AudioPlaybackModel Instance = new AudioPlaybackModel(16000, 1);
    }
}
