using Concentus.Structs;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using VoiceCraft.Core.Audio;

namespace VoiceCraft.Core.Client
{
    public class VoiceCraftParticipant
    {
        private float volume = 1.0f;
        private float proximityVolume = 0.0f;

        public bool Muted = false;
        public bool Deafened = false;
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

        public VoiceCraftJitterBuffer AudioBuffer;
        public Wave16ToFloatProvider FloatProvider;
        public EchoSampleProvider EchoProvider;
        public LowpassSampleProvider LowpassProvider;
        public MonoToStereoSampleProvider AudioProvider { get; }
        public OpusDecoder OpusDecoder { get; }

        public VoiceCraftParticipant(string Name, WaveFormat WaveFormat, int RecordLengthMS)
        {
            this.Name = Name;

            //Setup and wire everything up.
            OpusDecoder = new OpusDecoder(WaveFormat.SampleRate, WaveFormat.Channels);
            AudioBuffer = new VoiceCraftJitterBuffer(OpusDecoder, WaveFormat, RecordLengthMS);
            FloatProvider = new Wave16ToFloatProvider(AudioBuffer);
            EchoProvider = new EchoSampleProvider(FloatProvider.ToSampleProvider());
            LowpassProvider = new LowpassSampleProvider(EchoProvider, 200, 1);
            AudioProvider = new MonoToStereoSampleProvider(LowpassProvider);
        }

        public void AddAudioSamples(byte[] Audio, uint PacketCount)
        {
            AudioBuffer.AddSamples(Audio, PacketCount);
        }

        //Private Methods
        private void UpdateVolume()
        {
            FloatProvider.Volume = proximityVolume * volume;
        }
    }
}