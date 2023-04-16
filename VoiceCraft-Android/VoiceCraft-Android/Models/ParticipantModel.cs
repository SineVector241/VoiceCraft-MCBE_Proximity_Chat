using NAudio.Wave;

namespace VoiceCraft_Android.Models
{
    public class ParticipantModel
    {
        public string Name { get; set; }
        public string LoginKey { get; set; }
        public BufferedWaveProvider WaveProvider { get; set; }
    }
}
