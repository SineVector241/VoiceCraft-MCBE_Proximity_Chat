using VoiceCraft.Core.Packets.Interfaces;

namespace VoiceCraft.Core.Packets.MCComm
{
    public class Bind : IMCCommPacketData
    {
        public string PlayerId { get; set; } = string.Empty;
        public ushort PlayerKey { get; set; }
        public string Gamertag { get; set; } = string.Empty;
    }
}
