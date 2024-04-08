namespace VoiceCraft.Core.Packets.MCComm
{
    public class Bind : MCCommPacket
    {
        public override byte PacketId => (byte)MCCommPacketTypes.Bind;

        public string PlayerId { get; set; } = string.Empty;
        public short PlayerKey { get; set; }
        public string Gamertag { get; set; } = string.Empty;
    }
}
