using Concentus.Structs;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Diagnostics;
using VoiceCraft.Core.Audio;

namespace VoiceCraft.Core.Client
{
    public class VoiceCraftParticipant : IDisposable
    {
        private float volume = 1.0f;
        private float proximityVolume = 0.0f;

        public bool Muted = false;
        public bool Deafened = false;
        public bool IsDisposed { get; private set; } = false;
        public DateTime LastSpoke { get; private set; }
        public string Name { get; }

        public float Volume
        {
            get { return volume; }
            set
            {
                volume = value;
                UpdateVolume();
            }
        }
        public float ProximityVolume
        {
            get { return proximityVolume; }
            set
            {
                proximityVolume = value;
                UpdateVolume();
            }
        }

        public BufferedWaveProvider AudioBuffer { get; }
        public Wave16ToFloatProvider FloatProvider { get; }
        public EchoSampleProvider EchoProvider { get; }
        public LowpassSampleProvider LowpassProvider { get; }
        public MonoToStereoSampleProvider AudioProvider { get; }
        public OpusDecoder OpusDecoder { get; }
        private VoiceCraftJitterBuffer NetworkJitterBuffer;
        private System.Timers.Timer DecodeInterval;

        public VoiceCraftParticipant(string Name, WaveFormat WaveFormat, int RecordLengthMS)
        {
            this.Name = Name;
            DecodeInterval = new System.Timers.Timer(RecordLengthMS / 2);
            DecodeInterval.Elapsed += DecodeAudio;

            //Setup and wire everything up.
            OpusDecoder = new OpusDecoder(WaveFormat.SampleRate, WaveFormat.Channels);
            NetworkJitterBuffer = new VoiceCraftJitterBuffer(OpusDecoder, WaveFormat, RecordLengthMS);
            AudioBuffer = new BufferedWaveProvider(WaveFormat) { ReadFully = true, BufferDuration = TimeSpan.FromSeconds(2)};
            FloatProvider = new Wave16ToFloatProvider(AudioBuffer);
            EchoProvider = new EchoSampleProvider(FloatProvider.ToSampleProvider());
            LowpassProvider = new LowpassSampleProvider(EchoProvider, 200, 1);
            AudioProvider = new MonoToStereoSampleProvider(LowpassProvider);

            DecodeInterval.Start();
        }

        public void AddAudioSamples(byte[] Audio, uint PacketCount)
        {
            NetworkJitterBuffer.Put(Audio, PacketCount);
            LastSpoke = DateTime.UtcNow;
        }

        //Private Methods
        private void UpdateVolume()
        {
            FloatProvider.Volume = proximityVolume * volume;
        }

        private void DecodeAudio(object sender, System.Timers.ElapsedEventArgs e)
        {
            var decodedBytes = new byte[NetworkJitterBuffer.DecodeBufferSize];
            var decoded = NetworkJitterBuffer.Get(decodedBytes);
            if(decoded != -1)
            {
                AudioBuffer.AddSamples(decodedBytes, 0, decoded);
            }
        }

        ~VoiceCraftParticipant()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    DecodeInterval.Stop();
                    DecodeInterval.Dispose();
                    IsDisposed = true;
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}