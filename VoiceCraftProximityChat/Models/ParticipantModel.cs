using NAudio.Wave;

namespace VoiceCraftProximityChat.Models
{
    public class ParticipantModel
    {
        public string Name { get; set; } = "";
        public string LoginKey { get; set; } = "";
        public BufferedWaveProvider WaveProvider { get; set; }
        public Wave16ToFloatProvider FloatProvider { get; set; }
    }
}
