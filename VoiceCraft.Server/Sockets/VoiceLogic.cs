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
            if (participant != null)
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
            if (audioparticipant.Value != null && audioparticipant.Value.Binded && !audioparticipant.Value.Muted && ServerProperties.Properties.ProximityToggle)
            {
                var list = ServerData.Participants.Where(x => x.Value != null && x.Value.Binded && x.Value.MinecraftData.PlayerId != audioparticipant.Value.MinecraftData.PlayerId && x.Value.MinecraftData.DimensionId == audioparticipant.Value.MinecraftData.DimensionId && Vector3.Distance(x.Value.MinecraftData.Position, audioparticipant.Value.MinecraftData.Position) <= ServerProperties.Properties.ProximityDistance);
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
                        var vec = new Vector3((float)(cosTheta * distance.X - sinTheta * distance.Z), 0, (float)(sinTheta * distance.X - cosTheta * distance.Z));

                        Packet.PacketPosition = vec;
                        Packet.PacketKey = participant.Key;
                        SendPacket(Packet, participant.Value.SocketData.VoiceAddress);
                    }
                }
            }
        }

        private void HandleUpdate(VoicePacket Packet, EndPoint EP)
        {
            var participant = ServerData.GetParticipantByVoice(EP);
            if(participant.Value != null && Packet.PacketEnviromentId != null)
            {
                participant.Value.MinecraftData.Position = Packet.PacketPosition;
                participant.Value.MinecraftData.DimensionId = Packet.PacketEnviromentId;
            }
        }
    }
}
