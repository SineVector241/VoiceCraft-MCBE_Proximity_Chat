using System.Numerics;

namespace VoiceCraft_Mobile.Models
{
    public class ParticipantModel
    {
        public string Name { get; set; }
        public string Ip { get; set; }
        public int Port { get; set; }
        public string UserId { get; set; }
        public Vector3 Postition { get; set; }
    }
}
