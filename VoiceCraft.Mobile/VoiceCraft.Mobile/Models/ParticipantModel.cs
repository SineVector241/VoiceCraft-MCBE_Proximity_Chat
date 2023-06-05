using Concentus.Structs;
using NAudio.Codecs;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace VoiceCraft.Mobile.Models
{
    public class ParticipantModel
    {
        public string Name { get; set; }
        public uint Id { get; set; }
        public uint PacketCount { get; set; }
        public BufferedWaveProvider WaveProvider { get; }
        public Wave16ToFloatProvider FloatProvider { get; }
        public MonoToStereoSampleProvider MonoToStereo { get; }

#nullable enable
        public OpusDecoder? OpusDecoder { get; }
        public G722Codec? G722Decoder { get; }

        public ParticipantModel(string Name, uint Id, WaveFormat WaveFormat)
        {
            this.Name = Name;
            this.Id = Id;

            WaveProvider = new BufferedWaveProvider(WaveFormat);
            FloatProvider = new Wave16ToFloatProvider(WaveProvider);
            MonoToStereo = new MonoToStereoSampleProvider(FloatProvider.ToSampleProvider());
        }
    }
}