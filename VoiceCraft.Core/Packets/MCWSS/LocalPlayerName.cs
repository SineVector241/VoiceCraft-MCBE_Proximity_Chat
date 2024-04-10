namespace VoiceCraft.Core.Packets.MCWSS
{
    public class LocalPlayerName
    {
        public string localplayername { get; set; } = string.Empty;
        public int statusCode { get; set; }
        public string statusMessage { get; set; } = string.Empty;
    }
}
