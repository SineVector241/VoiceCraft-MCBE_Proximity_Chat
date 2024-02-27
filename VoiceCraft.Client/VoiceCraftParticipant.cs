using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using VoiceCraft.Core;
using VoiceCraft.Core.Audio.Streams;
using VoiceCraft.Core.Audio;
using System;

namespace VoiceCraft.Client
{
    public class VoiceCraftParticipant : Participant, IDisposable
    {
        private float volume = 1.0f;
        private float proximityVolume = 0.0f;

        public bool IsDisposed { get; private set; }
        public WaveFormat AudioFormat { get; }
        public int FrameSizeMS { get; }
        public bool IsMuted { get; set; }
        public bool IsDeafened { get; set; }
        public bool Muffled { get => LowpassProvider.Enabled; set => LowpassProvider.Enabled = value; }
        public float EchoFactor { get => EchoProvider.EchoFactor; set => EchoProvider.EchoFactor = value; }
        public float RightVolume { get => AudioOutput.RightVolume; set => AudioOutput.RightVolume = value; }
        public float LeftVolume { get => AudioOutput.LeftVolume; set => AudioOutput.LeftVolume = value; }
        public float Volume
        {
            get { return volume; }
            set
            {
                volume = value;
                FloatProvider.Volume = proximityVolume * volume;
            }
        }
        public float ProximityVolume
        {
            get { return proximityVolume; }
            set
            {
                proximityVolume = value;
                FloatProvider.Volume = proximityVolume * volume;
            }
        }

        private VoiceCraftJitterBuffer JitterBuffer { get; }
        private VoiceCraftStream VoiceCraftStream { get; }
        private Wave16ToFloatProvider FloatProvider { get; }
        private EchoSampleProvider EchoProvider { get; }
        private LowpassSampleProvider LowpassProvider { get; }
        public MonoToStereoSampleProvider AudioOutput { get; }

        public VoiceCraftParticipant(string name, WaveFormat audioFormat, int frameSizeMS) : base(name)
        {
            AudioFormat = audioFormat;
            FrameSizeMS = frameSizeMS;
            IsMuted = false;
            IsDeafened = false;

            //Setup and wire everything up.
            JitterBuffer = new VoiceCraftJitterBuffer(AudioFormat);
            VoiceCraftStream = new VoiceCraftStream(AudioFormat, JitterBuffer);
            FloatProvider = new Wave16ToFloatProvider(VoiceCraftStream);
            EchoProvider = new EchoSampleProvider(FloatProvider.ToSampleProvider());
            LowpassProvider = new LowpassSampleProvider(EchoProvider, 200, 1);
            AudioOutput = new MonoToStereoSampleProvider(LowpassProvider);
        }

        public void AddSamples(byte[] audio, uint packetCount)
        {
            LastActive = Environment.TickCount;
            JitterBuffer.Put(audio, packetCount);
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
