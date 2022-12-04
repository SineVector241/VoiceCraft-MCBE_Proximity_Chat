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

        public AudioPlaybackModel()
        {
            outputDevice = new WaveOutEvent();
            mixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(16000, 1));
            waveProvider = new BufferedWaveProvider(new WaveFormat(16000,1));
            waveProvider.DiscardOnBufferOverflow = false;
            waveProvider.ReadFully = false;
            waveProvider.BufferLength = 1024 * 16;
            mixer.ReadFully = true;
            outputDevice.Init(mixer);
            outputDevice.Play();
        }

        public void PlaySound(byte[] buffer, float Volume)
        {
            Task.Run(() =>
            {
                var provider = new RawSourceWaveStream(buffer, 0, 1600, new WaveFormat(16000, 1));
                waveProvider.AddSamples(buffer, 0, 1600);
                var buff = new Wave16ToFloatProvider(waveProvider);
                buff.Volume = Volume;
                mixer.AddMixerInput(buff);
            });
        }

        public static readonly AudioPlaybackModel Instance = new AudioPlaybackModel();
    }
}
