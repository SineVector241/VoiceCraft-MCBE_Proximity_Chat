using NAudio.Wave.SampleProviders;
using NAudio.Wave;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace VoiceCraftProximityChat.Models
{
    class AudioPlaybackModel
    {
        public AudioPlaybackModel()
        {
            /*
            outputDevice = new WaveOutEvent();
            mixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channelCount));
            mixer.ReadFully = true;
            outputDevice.Init(mixer);
            outputDevice.Play();
            */
        }

        public void PlaySound(byte[] bytes, float volume)
        {
            Task.Run(() =>
            {
                for (int i = 0; i < 20; i++)
                {
                    Buffer.SetByte(bytes, i, 0);
                }
                for (int i = 0; i < 20; i++)
                {
                    Buffer.SetByte(bytes, bytes.Length - (i + 1), 0);
                }
                var ms = new MemoryStream(bytes);
                var rs = new RawSourceWaveStream(ms, WaveFormat.CreateIeeeFloatWaveFormat(16000, 1));
                var wo = new DirectSoundOut();
                wo.Volume = volume;
                wo.Init(rs);
                wo.Play();
                while (wo.PlaybackState == PlaybackState.Playing)
                {
                    Thread.Sleep(50);
                }
                wo.Dispose();
            });
        }
    }
}
