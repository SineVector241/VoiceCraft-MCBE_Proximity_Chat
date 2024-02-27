using VoiceCraft.Core;

namespace VoiceCraft.Client
{
    public class VoiceCraftChannel : Channel
    {
        public VoiceCraftChannel(string name) : base(name)
        {
        }

        public bool RequiresPassword { get; set; } = false;
        public byte ChannelId { get; set; }
        public bool Joined { get; set; }
    }
}
