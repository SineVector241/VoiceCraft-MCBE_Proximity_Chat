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
            waveProvider = new BufferedWaveProvider(G722ChatCodec.CodecInstance.RecordFormat);
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
                var decoded = G722ChatCodec.CodecInstance.Decode(buffer, 0, 400);
                var provider = new RawSourceWaveStream(decoded, 0, 1600, G722ChatCodec.CodecInstance.RecordFormat);
                waveProvider.AddSamples(decoded, 0, 1600);
                var buff = new Wave16ToFloatProvider(waveProvider);
                buff.Volume = Volume;
                mixer.AddMixerInput(buff);
            });
        }

        public static readonly AudioPlaybackModel Instance = new AudioPlaybackModel();
    }
}
