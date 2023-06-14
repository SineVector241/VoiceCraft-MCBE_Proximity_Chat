using System.Net;
using VoiceCraft.Server.Helpers;
using VoiceCraft.Server.Network.Packets;

namespace VoiceCraft.Server.Sockets
{
    public partial class Signalling
    {
        private void HandleInfoPing(EndPoint EP)
        {
            var codec = "Hybrid";
            var connType = "Hybrid";
            switch(ServerProperties.Properties.Codec)
            {
                case AudioCodecs.Opus:
                    codec = "Opus";
                    break;
                case AudioCodecs.G722:
                    codec = "G722";
                    break;
            }

            switch (ServerProperties.Properties.ConnectionType)
            {
                case ConnectionTypes.Server:
                    connType = "Server";
                    break;
                case ConnectionTypes.Client:
                    connType = "Client";
                    break;
            }

            SendPacket(new SignallingPacket()
            {
                PacketIdentifier = SignallingPacketIdentifiers.InfoPing,
                PacketMetadata = $"MOTD: {ServerProperties.Properties.ServerMOTD}" +
                $"\nConnection Type: {connType}" +
                $"\nConnected Participants: {ServerData.Participants.Count}" +
                $"\nCodec: {codec}"
            }, EP);
        }

       private void HandleServerSidedLogin(SignallingPacket Packet, EndPoint EP)
        {
            if (Packet.PacketCodec == AudioCodecs.G722 && ServerProperties.Properties.Codec == AudioCodecs.Opus)
            {
                SendPacket(new SignallingPacket() 
                { 
                    PacketIdentifier = SignallingPacketIdentifiers.Deny,
                    PacketMetadata = "Server only accepts Opus audio codec!"
                }, EP);
                return;
            }
            else if (Packet.PacketCodec == AudioCodecs.Opus && ServerProperties.Properties.Codec == AudioCodecs.G722)
            {
                SendPacket(new SignallingPacket() 
                {
                    PacketIdentifier = SignallingPacketIdentifiers.Deny,
                    PacketMetadata = "Server only accepts G722 audio codec!"
                }, EP);
                return;
            }

            if (ServerProperties.Properties.ConnectionType == ConnectionTypes.Server ||
                ServerProperties.Properties.ConnectionType == ConnectionTypes.Hybrid)
            {
                Logger.LogToConsole(LogType.Info, $"Received Server Sided Login: {Packet.PacketKey}", nameof(Signalling));
                var key = Packet.PacketKey;
                var participant = new Participant();
                participant.SocketData.SignallingAddress = EP;
                participant.Codec = Packet.PacketCodec;

                if(ServerData.Participants.ContainsKey(Packet.PacketKey))
                {
                    for (ushort i = 0; i < ushort.MaxValue; i++)
                    {
                        if (!ServerData.Participants.ContainsKey(i))
                        {
                            key = i;
                            break;
                        }
                    }
                }

                ServerData.AddParticipant(key, participant);
                SendPacket(new SignallingPacket()
                {
                    PacketIdentifier = ServerProperties.Properties.Codec == AudioCodecs.Opus ? SignallingPacketIdentifiers.Accept48 : SignallingPacketIdentifiers.Accept16,
                    PacketKey = key,
                    PacketVoicePort = ServerProperties.Properties.VoicePortUDP
                }, EP);
            }
            else
            {
                SendPacket(new SignallingPacket()
                {
                    PacketIdentifier = SignallingPacketIdentifiers.Deny,
                    PacketMetadata = "Server only accepts client sided logins!"
                }, EP);

                Logger.LogToConsole(LogType.Warn, $"Login Denied: Key: {Packet.PacketKey} Reason: Server only accepts client sided logins.", nameof(Signalling));
            }
        }

        private void HandleClientSidedLogin(SignallingPacket Packet, EndPoint EP)
        {
            if (Packet.PacketCodec == AudioCodecs.G722 && ServerProperties.Properties.Codec == AudioCodecs.Opus)
            {
                SendPacket(new SignallingPacket()
                {
                    PacketIdentifier = SignallingPacketIdentifiers.Deny,
                    PacketMetadata = "Server only accepts Opus audio codec!"
                }, EP);
                return;
            }
            else if (Packet.PacketCodec == AudioCodecs.Opus && ServerProperties.Properties.Codec == AudioCodecs.G722)
            {
                SendPacket(new SignallingPacket()
                {
                    PacketIdentifier = SignallingPacketIdentifiers.Deny,
                    PacketMetadata = "Server only accepts G722 audio codec!"
                }, EP);
                return;
            }

            if (ServerProperties.Properties.ConnectionType == ConnectionTypes.Client ||
                ServerProperties.Properties.ConnectionType == ConnectionTypes.Hybrid)
            {
                Logger.LogToConsole(LogType.Info, $"Received Client Sided Login: {Packet.PacketKey}", nameof(Signalling));
                var key = Packet.PacketKey;
                var participant = new Participant();
                participant.SocketData.SignallingAddress = EP;
                participant.ClientSided = true;
                participant.Codec = Packet.PacketCodec;

                if (ServerData.Participants.ContainsKey(Packet.PacketKey))
                {
                    for (ushort i = 0; i < ushort.MaxValue; i++)
                    {
                        if (!ServerData.Participants.ContainsKey(i))
                        {
                            key = i;
                            break;
                        }
                    }
                }

                ServerData.AddParticipant(key, participant);
                SendPacket(new SignallingPacket()
                {
                    PacketIdentifier = ServerProperties.Properties.Codec == AudioCodecs.Opus ? SignallingPacketIdentifiers.Accept48 : SignallingPacketIdentifiers.Accept16,
                    PacketKey = key,
                    PacketVoicePort = ServerProperties.Properties.VoicePortUDP
                }, EP);
            }
            else
            {
                SendPacket(new SignallingPacket()
                {
                    PacketIdentifier = SignallingPacketIdentifiers.Deny,
                    PacketMetadata = "Server only accepts server sided logins!"
                }, EP);

                Logger.LogToConsole(LogType.Warn, $"Login Denied: Key: {Packet.PacketKey} Reason: Server only accepts server sided logins.", nameof(Signalling));
            }
        }

        private void HandleLogout(EndPoint EP)
        {
            var participant = ServerData.GetParticipantBySignalling(EP);
            if(participant.Value != null)
            {
                ServerData.RemoveParticipant(participant.Key, "logout");
            }
        }

        private void HandleBind(SignallingPacket Packet, EndPoint EP)
        {
            if(ServerProperties.Properties.ConnectionType == ConnectionTypes.Client ||
                ServerProperties.Properties.ConnectionType == ConnectionTypes.Hybrid)
            {
                var participant = ServerData.GetParticipantByKey(Packet.PacketKey);
                if(participant != null && Packet.PacketMetadata != null && participant.ClientSided)
                {
                    participant.Binded = true;
                    participant.MinecraftData.Gamertag = Packet.PacketMetadata; //Requires a name
                    SendPacket(Packet, EP); //Send the packet back for server confirmation...
                    ServerEvents.InvokeParticipantBinded(participant, Packet.PacketKey);
                }
            }

            //Else do nothing.
        }

        private void HandlePing(EndPoint EP)
        {
            var participant = ServerData.GetParticipantBySignalling(EP);
            if(participant.Value != null)
            {
                participant.Value.SocketData.LastPing = DateTime.UtcNow;
                SendPacket(new SignallingPacket() { PacketIdentifier = SignallingPacketIdentifiers.Ping }, EP);
            }
        }
    }
}
