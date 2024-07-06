namespace VoiceCraft.Core.Packets.MCComm
{
    public class GetParticipantBitmask : MCCommPacket
    {
        public override byte PacketId => (byte)MCCommPacketTypes.GetParticipantBitmask;
        public byte Bitmask { get; set; }
    }
}
