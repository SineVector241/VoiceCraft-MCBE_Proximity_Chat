﻿namespace VoiceCraft.Core.Packets.MCComm
{
    public class SetParticipantBitmask : MCCommPacket
    {
        public override byte PacketId => (byte)MCCommPacketTypes.SetParticipantBitmask;
        public string PlayerId { get; set; } = string.Empty;
        public bool IgnoreDataBitmask { get; set; }
        public uint Bitmask { get; set; }
    }
}
