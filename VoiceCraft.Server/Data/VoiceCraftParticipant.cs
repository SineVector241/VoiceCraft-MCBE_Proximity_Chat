using VoiceCraft.Core;
using VoiceCraft.Network;

namespace VoiceCraft.Server.Data
{
    public class VoiceCraftParticipant : Participant
    {
        public NetPeer NetPeer { get; }
        public VoiceCraftParticipant(string name, NetPeer peer) : base(name)
        {
            NetPeer = peer;
        }
    }
}
