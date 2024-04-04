using VoiceCraft.Core.Packets;
using VoiceCraft.Core.Packets.MCComm;

PacketRegistry registry = new PacketRegistry();

registry.RegisterPacket((byte)MCCommPacketId.Accept, typeof(Accept));
registry.RegisterPacket((byte)MCCommPacketId.Update, typeof(Update));

var mccomm = new Update();

Console.WriteLine(mccomm.SerializePacket(mccomm));