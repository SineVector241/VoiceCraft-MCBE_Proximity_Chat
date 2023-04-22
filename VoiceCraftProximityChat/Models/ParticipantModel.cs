using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace VoiceCraftProximityChat.Models
{
    public class ParticipantModel
    {
        public string Name { get; set; } = "";
        public string LoginKey { get; set; } = "";
        public BufferedWaveProvider WaveProvider { get; set; }
        public VolumeSampleProvider VolumeProvider { get; set; }
    }
}
