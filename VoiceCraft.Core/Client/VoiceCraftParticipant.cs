using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using VoiceCraft.Core.Audio;
using VoiceCraft.Core.Audio.Streams;

namespace VoiceCraft.Core.Client
{
    public class VoiceCraftParticipant : IDisposable
    {
        private JitterBufferPacket inPacket;
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

        public OpusStream OpusDecodeStream { get; }
        public JitterBuffer JitterBuffer { get; }
        public VoiceCraftStream VoiceCraftStream { get; }
        public Wave16ToFloatProvider FloatProvider { get; }
        public EchoSampleProvider EchoProvider { get; }
        public LowpassSampleProvider LowpassProvider { get; }
        public MonoToStereoSampleProvider AudioProvider { get; }

        public VoiceCraftParticipant(string Name, WaveFormat WaveFormat, int RecordLengthMS)
        {
            this.Name = Name;

            //Setup and wire everything up.
            JitterBuffer = new JitterBuffer(20);
            OpusDecodeStream = new OpusStream(WaveFormat, JitterBuffer);
            VoiceCraftStream = new VoiceCraftStream(WaveFormat, OpusDecodeStream);
            FloatProvider = new Wave16ToFloatProvider(VoiceCraftStream);
            EchoProvider = new EchoSampleProvider(FloatProvider.ToSampleProvider());
            LowpassProvider = new LowpassSampleProvider(EchoProvider, 200, 1);
            AudioProvider = new MonoToStereoSampleProvider(LowpassProvider);
        }

        public void AddAudioSamples(byte[] Audio, uint PacketCount)
        {
            inPacket.Data = Audio;
            inPacket.Sequence = PacketCount;
            inPacket.Length = Audio.Length;
            inPacket.Timestamp = DateTime.UtcNow;
            JitterBuffer.Put(inPacket);
            LastSpoke = DateTime.UtcNow;
        }

        //Private Methods
        private void UpdateVolume()
        {
            FloatProvider.Volume = proximityVolume * volume;
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
                    VoiceCraftStream.Dispose();
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