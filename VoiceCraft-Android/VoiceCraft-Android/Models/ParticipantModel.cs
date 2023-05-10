using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace VoiceCraft_Android.Models
{
    public class ParticipantModel
    {
        public string Name { get; set; }
        public string LoginKey { get; set; }
        public BufferedWaveProvider WaveProvider { get; set; }
        public Wave16ToFloatProvider FloatProvider { get; set; }
        public MonoToStereoSampleProvider MonoToStereo { get; set; }
    }
}
