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
            get { return FloatProvider.Volume; }
            set
            {
                FloatProvider.Volume = value;
            }
        }
        public float ProximityVolume
        {
            get { return SmoothVolumeProvider.TargetVolume; }
            set
            {
                SmoothVolumeProvider.TargetVolume = value;
            }
        }

        private VoiceCraftJitterBuffer JitterBuffer { get; }
        private VoiceCraftStream VoiceCraftStream { get; }
        private Wave16ToFloatProvider FloatProvider { get; }
        private SmoothVolumeSampleProvider SmoothVolumeProvider { get; }
        private EchoSampleProvider EchoProvider { get; }
        private LowpassSampleProvider LowpassProvider { get; }
        public MonoToStereoSampleProvider AudioOutput { get; }

        public VoiceCraftParticipant(string name, ushort publicId, WaveFormat audioFormat, int frameSizeMS) : base(name, publicId)
        {
            AudioFormat = audioFormat;
            FrameSizeMS = frameSizeMS;
            IsMuted = false;
            IsDeafened = false;
            LastActive = 0;

            //Setup and wire everything up.
            JitterBuffer = new VoiceCraftJitterBuffer(AudioFormat, frameSizeMS);
            VoiceCraftStream = new VoiceCraftStream(AudioFormat, JitterBuffer);
            FloatProvider = new Wave16ToFloatProvider(VoiceCraftStream);
            SmoothVolumeProvider = new SmoothVolumeSampleProvider(FloatProvider.ToSampleProvider(), 20);
            EchoProvider = new EchoSampleProvider(SmoothVolumeProvider, 120) { DecayFactor = 0.1f };
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
