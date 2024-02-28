namespace VoiceCraft.Data.Server
{
    public class ExternalServer
    {
        public int LastActive { get; set; } = Environment.TickCount;
        public string IP { get; set; } = string.Empty;
    }
}
