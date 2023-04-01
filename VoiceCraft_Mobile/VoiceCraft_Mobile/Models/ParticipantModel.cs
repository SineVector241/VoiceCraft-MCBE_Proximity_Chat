using NAudio.Wave;

namespace VoiceCraft_Mobile.Models
{
    public class ParticipantModel
    {
        public string Name { get; set; }
        public string LoginId { get; set; }
        public BufferedWaveProvider WaveProvider { get; set; }
    }
}
