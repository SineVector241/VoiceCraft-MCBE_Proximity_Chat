using VoiceCraft.Core;
using VoiceCraft.Network;

namespace VoiceCraft.Server.Data
{
    public class VoiceCraftParticipant : Participant
    {
        public bool Binded { get; set; }
        public bool ClientSided { get; set; }
        public bool Deafened { get; set; }
        public bool Muted { get; set; }
        public short Key { get; set; }
        public Channel? Channel { get; set; }
        public VoiceCraftParticipant(string name) : base(name)
        {
        }

        public static short GenerateKey()
        {
            return (short)Random.Shared.Next(short.MinValue + 1, short.MaxValue); //short.MinValue is used to specify no Key.
        }
    }
}
