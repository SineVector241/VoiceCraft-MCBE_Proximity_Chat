namespace VoiceCraft.Core.Packets.MCComm
{
    public class Bind : MCCommPacket
    {
        public override byte PacketId => (byte)MCCommPacketId.Bind;

        public string PlayerId { get; set; } = string.Empty;
        public ushort PlayerKey { get; set; }
        public string Gamertag { get; set; } = string.Empty;
    }
}
