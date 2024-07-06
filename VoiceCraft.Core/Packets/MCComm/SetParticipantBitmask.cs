namespace VoiceCraft.Core.Packets.MCComm
{
    public class SetParticipantBitmask : MCCommPacket
    {
        public override byte PacketId => (byte)MCCommPacketTypes.SetParticipantBitmask;
        public byte Bitmask { get; set; }
    }
}
