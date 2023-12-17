namespace VoiceCraft.Core.Packets.MCComm
{
    public class Bind
    {
        public string PlayerId { get; set; } = string.Empty;
        public ushort PlayerKey { get; set; }
        public string Gamertag { get; set; } = string.Empty;
    }
}
