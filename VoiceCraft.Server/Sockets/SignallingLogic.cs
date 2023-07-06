using System.Net;
using VoiceCraft.Server.Helpers;
using VoiceCraft.Server.Network.Packets;

namespace VoiceCraft.Server.Sockets
{
    public partial class Signalling
    {
        private void HandleInfoPing(EndPoint EP)
        {
            var connType = "Hybrid";

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
                $"\nConnected Participants: {ServerData.Participants.Count}"
            }, EP);
        }

       private void HandleServerSidedLogin(SignallingPacket Packet, EndPoint EP)
        {
            if (ServerProperties.Properties.ConnectionType == ConnectionTypes.Server ||
                ServerProperties.Properties.ConnectionType == ConnectionTypes.Hybrid)
            {
                Logger.LogToConsole(LogType.Info, $"Received Server Sided Login: {Packet.PacketKey}", nameof(Signalling));
                var key = Packet.PacketKey;
                var participant = new Participant();
                participant.SocketData.SignallingAddress = EP;

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
                    PacketIdentifier = SignallingPacketIdentifiers.Accept,
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
            if (ServerProperties.Properties.ConnectionType == ConnectionTypes.Client ||
                ServerProperties.Properties.ConnectionType == ConnectionTypes.Hybrid)
            {
                Logger.LogToConsole(LogType.Info, $"Received Client Sided Login: {Packet.PacketKey}", nameof(Signalling));
                var key = Packet.PacketKey;
                var participant = new Participant();
                participant.SocketData.SignallingAddress = EP;
                participant.ClientSided = true;

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
                    PacketIdentifier = SignallingPacketIdentifiers.Accept,
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

        private void HandleDeafen(EndPoint EP)
        {
            var participant = ServerData.GetParticipantBySignalling(EP);
            if(participant.Value != null) 
            { 
                participant.Value.Deafened = true;
            }
        }

        private void HandleUndeafen(EndPoint EP)
        {
            var participant = ServerData.GetParticipantBySignalling(EP);
            if (participant.Value != null)
            {
                participant.Value.Deafened = false;
            }
        }
    }
}
