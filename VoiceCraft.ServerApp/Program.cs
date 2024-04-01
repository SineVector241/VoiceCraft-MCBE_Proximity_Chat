using VoiceCraft.Network.Packets;
using VoiceCraft.Network.Packets.VoiceCraft;

Console.WriteLine("Registering Packet...");
PacketRegistry.RegisterPacket((byte)VoiceCraftPacketTypes.Login, typeof(Login));
PacketRegistry.RegisterPacket((byte)VoiceCraftPacketTypes.Logout, typeof(Logout));
PacketRegistry.RegisterPacket((byte)VoiceCraftPacketTypes.Accept, typeof(Accept));
PacketRegistry.RegisterPacket((byte)VoiceCraftPacketTypes.Deny, typeof(Deny));
PacketRegistry.RegisterPacket((byte)VoiceCraftPacketTypes.Ping, typeof(Ping));
PacketRegistry.RegisterPacket((byte)VoiceCraftPacketTypes.PingInfo, typeof(PingInfo));
PacketRegistry.RegisterPacket((byte)VoiceCraftPacketTypes.ParticipantJoined, typeof(ParticipantJoined));
PacketRegistry.RegisterPacket((byte)VoiceCraftPacketTypes.ParticipantLeft, typeof(ParticipantLeft));
PacketRegistry.RegisterPacket((byte)VoiceCraftPacketTypes.Mute, typeof(Mute));
PacketRegistry.RegisterPacket((byte)VoiceCraftPacketTypes.Unmute, typeof(Unmute));
PacketRegistry.RegisterPacket((byte)VoiceCraftPacketTypes.Deafen, typeof(Deafen));
PacketRegistry.RegisterPacket((byte)VoiceCraftPacketTypes.Undeafen, typeof(Undeafen));
PacketRegistry.RegisterPacket((byte)VoiceCraftPacketTypes.JoinChannel, typeof(JoinChannel));
PacketRegistry.RegisterPacket((byte)VoiceCraftPacketTypes.LeaveChannel, typeof(LeaveChannel));
PacketRegistry.RegisterPacket((byte)VoiceCraftPacketTypes.AddChannel, typeof(AddChannel));
PacketRegistry.RegisterPacket((byte)VoiceCraftPacketTypes.RemoveChannel, typeof(RemoveChannel));

var packet = new JoinChannel() { ChannelId = 2, Id = 1192837891273, Password = "AAEEEIII" };
var stream = new List<byte>();
packet.WritePacket(ref stream);
var data = stream.ToArray();
Console.WriteLine(stream.ToArray().Length);

var packetData = PacketRegistry.GetPacketFromDataStream(data);
Console.WriteLine(((JoinChannel)packetData).Password);