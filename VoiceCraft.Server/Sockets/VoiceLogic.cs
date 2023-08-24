using System.Net;
using System.Numerics;
using VoiceCraft.Server.Helpers;
using VoiceCraft.Server.Network.Packets;

namespace VoiceCraft.Server.Sockets
{
    public partial class Voice
    {
        private void HandleLogin(VoicePacket Packet, EndPoint EP)
        {
            Logger.LogToConsole(LogType.Info, $"New Login: Key: {Packet.PacketKey}", nameof(Voice));
            var participant = ServerData.GetParticipantByKey(Packet.PacketKey);
            if (participant != null && participant.SocketData.VoiceAddress == null)
            {
                participant.SocketData.VoiceAddress = EP;
                participant.SocketData.LastPing = DateTime.UtcNow;
                SendPacket(new VoicePacket() { PacketIdentifier = VoicePacketIdentifier.Accept }, EP);
                Logger.LogToConsole(LogType.Success, $"Login Accepted: Key: {Packet.PacketKey}", nameof(Voice));
                return;
            }

            SendPacket(new VoicePacket() { PacketIdentifier = VoicePacketIdentifier.Deny }, EP);
            Logger.LogToConsole(LogType.Warn, $"Login Denied: Key: {Packet.PacketKey}", nameof(Voice));
        }

        private void HandleAudio(VoicePacket Packet, EndPoint EP)
        {
            var audioparticipant = ServerData.GetParticipantByVoice(EP);
            if (audioparticipant.Value != null && audioparticipant.Value.Binded && !audioparticipant.Value.Muted && !audioparticipant.Value.Deafened)
            {
                if (ServerProperties.Properties.ProximityToggle)
                {
                    if (audioparticipant.Value.MinecraftData.IsDead) return;

                    var list = ServerData.Participants.Where(x => x.Value != null && x.Value.Binded && !x.Value.Deafened && x.Key != audioparticipant.Key && x.Value.MinecraftData.DimensionId != "void" && x.Value.MinecraftData.DimensionId == audioparticipant.Value.MinecraftData.DimensionId && Vector3.Distance(x.Value.MinecraftData.Position, audioparticipant.Value.MinecraftData.Position) <= ServerProperties.Properties.ProximityDistance && !x.Value.MinecraftData.IsDead);
                    for (int i = 0; i < list.Count(); i++)
                    {
                        var participant = list.ElementAt(i);
                        if (participant.Value?.SocketData.VoiceAddress != null)
                        {
                            var LocalPlayerCoordinates = participant.Value.MinecraftData.Position;
                            var AudioSourceCoordinates = audioparticipant.Value.MinecraftData.Position;
                            var LocalPlayerRotation = participant.Value.MinecraftData.Rotation;
                            var distance = Vector3.Subtract(LocalPlayerCoordinates, AudioSourceCoordinates);
                            var rotationToSource = Math.Atan2(0 - AudioSourceCoordinates.X, 0 - AudioSourceCoordinates.Z) - (LocalPlayerRotation * (Math.PI / 180));
                            var cosTheta = Math.Cos(rotationToSource);
                            var sinTheta = Math.Sin(rotationToSource);
                            var vec = new Vector3((float)(cosTheta * distance.X - sinTheta * distance.Z), AudioSourceCoordinates.Y - LocalPlayerCoordinates.Y, (float)(sinTheta * distance.X - cosTheta * distance.Z));

                            Packet.PacketPosition = vec;
                            Packet.PacketKey = audioparticipant.Key;
                            Packet.PacketDistance = (ushort)ServerProperties.Properties.ProximityDistance;
                            if(ServerProperties.Properties.VoiceEffects)
                                Packet.PacketEchoFactor = audioparticipant.Value.MinecraftData.CaveDensity * (Vector3.Distance(LocalPlayerCoordinates, AudioSourceCoordinates) / ServerProperties.Properties.ProximityDistance);
                            SendPacket(Packet, participant.Value.SocketData.VoiceAddress);
                        }
                    }
                }
                else
                {
                    var list = ServerData.Participants.Where(x => x.Value != null && x.Value.Binded && x.Key != audioparticipant.Key);
                    for (int i = 0; i < list.Count(); i++)
                    {
                        var participant = list.ElementAt(i);
                        if (participant.Value?.SocketData.VoiceAddress != null)
                        {
                            Packet.PacketPosition = new Vector3();
                            Packet.PacketKey = audioparticipant.Key;
                            Packet.PacketDistance = (ushort)ServerProperties.Properties.ProximityDistance;
                            SendPacket(Packet, participant.Value.SocketData.VoiceAddress);
                        }
                    }
                }
            }

            //Do nothing. Do not send audio. Do not handle it at all.
        }

        private void HandleUpdate(VoicePacket Packet, EndPoint EP)
        {
            var participant = ServerData.GetParticipantByVoice(EP);
            if(participant.Value != null && Packet.PacketEnviromentId != null && participant.Value.ClientSided)
            {
                //Do not need to do a check since MCWSS only sends update packets when the player moves. Packet.PacketPosition will always be different everytime a packet is received.
                participant.Value.MinecraftData.Position = Packet.PacketPosition;

                if(participant.Value.MinecraftData.DimensionId != Packet.PacketEnviromentId)
                    participant.Value.MinecraftData.DimensionId = Packet.PacketEnviromentId;
            }
        }
    }
}
