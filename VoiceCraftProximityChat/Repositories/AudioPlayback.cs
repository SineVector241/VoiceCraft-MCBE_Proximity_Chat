using NAudio.Wave.SampleProviders;
using NAudio.Wave;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using VoiceCraftProximityChat.Utils;
using System.Collections.Generic;
using System.Linq;

namespace VoiceCraftProximityChat.Repositories
{
    class AudioPlayback
    {
        private readonly IWavePlayer outputDevice;
        private readonly MixingSampleProvider mixer;
        private List<Client> waveProviders = new List<Client>();
        public static float volumeGain { get; set; } = 0.0f;

        public AudioPlayback()
        {
            outputDevice = new WaveOutEvent() { DesiredLatency = 600, NumberOfBuffers = 3 };
            mixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(16000, 1));
            mixer.ReadFully = true;
            outputDevice.Init(mixer);
            outputDevice.Play();
        }

        public void PlaySound(byte[] buffer, float Volume, string SessionKey)
        {
            Task.Factory.StartNew(() =>
            {
                var waveProvider = waveProviders.FirstOrDefault(x => x.SessionKey == SessionKey);
                if (waveProvider == null)
                {
                    waveProvider = new Client() { SessionKey = SessionKey };
                    waveProviders.Add(waveProvider);
                }
                waveProvider.LastUsed = DateTime.UtcNow;

                var decoded = G722ChatCodec.CodecInstance.Decode(buffer, 0, 400);
                var provider = new RawSourceWaveStream(decoded, 0, 1600, G722ChatCodec.CodecInstance.RecordFormat);
                waveProvider.waveProvider.AddSamples(decoded, 0, 1600);
                var buff = new Wave16ToFloatProvider(waveProvider.waveProvider);
                buff.Volume = Volume + volumeGain;
                mixer.AddMixerInput(buff);

                for (int i = 0; i < waveProviders.Count; i++)
                {
                    if ((DateTime.UtcNow - waveProviders[i].LastUsed).Seconds > 60)
                    {
                        waveProviders.RemoveAt(i);
                    }
                }
            });
        }

        public static readonly AudioPlayback Instance = new AudioPlayback();
    }

    public class Client
    {
        public BufferedWaveProvider waveProvider { get; set; } = new BufferedWaveProvider(G722ChatCodec.CodecInstance.RecordFormat) { DiscardOnBufferOverflow = false, ReadFully = false };
        public string SessionKey { get; set; } = "";
        public DateTime LastUsed { get; set; } = DateTime.UtcNow;
    }
}
