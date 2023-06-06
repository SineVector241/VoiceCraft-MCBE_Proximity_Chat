namespace VoiceCraft.Mobile.Models
{
    public class ServerModel
    {
        public string Name { get; set; } = string.Empty;
        public string IP { get; set; } = string.Empty;
        public int Port { get; set; } = 9050;
        public uint Key { get; set; }
        public int Codec { get; set; } = -1;
        public bool ClientSided { get; set; }
    }

    public enum Codecs
    {
        Opus,
        G722
    }
}
