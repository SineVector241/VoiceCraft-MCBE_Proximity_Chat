using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using VoiceCraft.Core;
using VoiceCraft.Core.Packets;
using VoiceCraft.Core.Packets.VoiceCraft;
using VoiceCraft.Network;
using VoiceCraft.Server.Data;

namespace VoiceCraft.Server
{
    //Client - Requesting Participant
    //Participant - Receiving Participant
    public class VoiceCraftServer
    {
        public const string Version = "v1.0.4";
        public ConcurrentDictionary<NetPeer, VoiceCraftParticipant> Participants { get; set; } = new ConcurrentDictionary<NetPeer, VoiceCraftParticipant>();
        public Network.Sockets.VoiceCraft VoiceCraftSocket { get; set; }
        public Network.Sockets.MCComm MCComm { get; set; }
        public Properties ServerProperties { get; set; }
        public List<string> Banlist { get; set; }

        #region Delegates
        public delegate void ParticipantJoined(VoiceCraftParticipant participant);
        public delegate void ParticipantLeft(VoiceCraftParticipant participant, string? reason = null);
        public delegate void ParticipantBinded(VoiceCraftParticipant participant);
        public delegate void Failed(Exception ex);
        #endregion

        #region Events
        public event ParticipantJoined? OnParticipantJoined;
        public event ParticipantLeft? OnParticipantLeft;
        public event ParticipantBinded? OnParticipantBinded;
        public event Failed? OnFailed;
        #endregion

        public VoiceCraftServer(Properties properties, List<string> banlist)
        {
            ServerProperties = properties;
            Banlist = banlist;
            VoiceCraftSocket = new Network.Sockets.VoiceCraft();
            MCComm = new Network.Sockets.MCComm();

            VoiceCraftSocket.OnPingInfoReceived += OnPingInfo;
            VoiceCraftSocket.OnPeerConnected += OnPeerConnected;
            VoiceCraftSocket.OnPeerDisconnected += OnPeerDisconnected;
            VoiceCraftSocket.OnBindedReceived += OnBinded;
            VoiceCraftSocket.OnMuteReceived += OnMute;
            VoiceCraftSocket.OnUnmuteReceived += OnUnmute;
            VoiceCraftSocket.OnDeafenReceived += OnDeafen;
            VoiceCraftSocket.OnUndeafenReceived += OnUndeafen;
            VoiceCraftSocket.OnJoinChannelReceived += OnJoinChannel;
            VoiceCraftSocket.OnLeaveChannelReceived += OnLeaveChannel;
        }

        #region Methods
        public void Start()
        {
            _ = Task.Run(async () => {
                try
                {
                    await VoiceCraftSocket.HostAsync(ServerProperties.VoiceCraftPortUDP);
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    OnFailed?.Invoke(ex);
                }
            });
        }

        public void Broadcast(VoiceCraftPacket packet, bool bindedOnly = true, VoiceCraftParticipant? excludeParticipant = null)
        {
            var list = Participants.Where(x => x.Value != excludeParticipant);
            foreach (var participant in list)
            {
                if(participant.Value.Binded && bindedOnly || !bindedOnly)
                {
                    participant.Key.AddToSendBuffer(packet);
                }
            }
        }

        private short GetAvailableKey(short preferredKey)
        {
            while (KeyExists(preferredKey))
            {
                preferredKey = VoiceCraftParticipant.GenerateKey();
            }
            return preferredKey;
        }

        private bool KeyExists(short key)
        {
            foreach (var participant in Participants)
            {
                if (participant.Value.Key == key) return true;
            }
            return false;
        }
        #endregion

        #region VoiceCraft Event Methods
        private void OnPingInfo(PingInfo data, NetPeer peer)
        {
            var connType = PositioningTypes.Unknown;

            switch (ServerProperties.ConnectionType)
            {
                case ConnectionTypes.Server:
                    connType = PositioningTypes.ServerSided;
                    break;
                case ConnectionTypes.Client:
                    connType = PositioningTypes.ClientSided;
                    break;
            }
            peer.AddToSendBuffer(new PingInfo() { ConnectedParticipants = Participants.Count, MOTD = ServerProperties.ServerMOTD, PositioningType = connType});
        }

        private void OnPeerConnected(NetPeer peer, Login packet)
        {
            if (Version != packet.Version)
            {
                peer.DenyLogin("Versions do not match!");
                return;
            }
            if (Banlist.Exists(x => x == ((IPEndPoint)peer.RemoteEndPoint).Address.ToString()))
            {
                peer.DenyLogin("You have been banned from the server!");
                return;
            }
            if (packet.PositioningType != PositioningTypes.ClientSided &&
                (ServerProperties.ConnectionType == ConnectionTypes.Client || ServerProperties.ConnectionType == ConnectionTypes.Hybrid))
            {
                peer.DenyLogin("Server only accepts client sided positioning!");
                return;
            }
            else if (packet.PositioningType != PositioningTypes.ServerSided &&
                (ServerProperties.ConnectionType == ConnectionTypes.Server || ServerProperties.ConnectionType == ConnectionTypes.Hybrid))
            {
                peer.DenyLogin("Server only accepts server sided positioning!");
                return;
            }
            var participant = new VoiceCraftParticipant(string.Empty);
            participant.ClientSided = PositioningTypes.ClientSided == packet.PositioningType;
            participant.Key = GetAvailableKey(packet.Key);
            peer.AcceptLogin(participant.Key);
            Participants.TryAdd(peer, participant);
            OnParticipantJoined?.Invoke(participant);
        }

        private void OnPeerDisconnected(NetPeer peer, string? reason = null)
        {
            if(Participants.TryRemove(peer, out var participant))
            {
                OnParticipantLeft?.Invoke(participant, reason);
            }
        }

        private void OnBinded(Binded data, NetPeer peer)
        {
            if (Participants.TryGetValue(peer, out var client) && client.ClientSided)
            {
                client.Name = data.Name;
                client.Binded = true;

                Broadcast(new Core.Packets.VoiceCraft.ParticipantJoined()
                {
                    IsDeafened = client.Deafened,
                    IsMuted = client.Muted,
                    Key = client.Key,
                    Name = client.Name
                }); //Broadcast to all other participants.

                var list = Participants.Where(x => x.Value != client);
                foreach (var participant in list)
                {
                    participant.Key.AddToSendBuffer(new Core.Packets.VoiceCraft.ParticipantJoined()
                    {
                        IsDeafened = participant.Value.Deafened,
                        IsMuted = participant.Value.Muted,
                        Key = participant.Value.Key,
                        Name = participant.Value.Name
                    });
                } //Send participants back to binded client.

                //Send channels to client...

                OnParticipantBinded?.Invoke(client);
            }
        }

        private void OnMute(Mute data, NetPeer peer)
        {
            if (Participants.TryGetValue(peer, out var client) && client.ClientSided)
            {
                if (!client.Binded) return;

                Broadcast(new Mute() { Key = client.Key }, true, client);
            }
        }

        private void OnUnmute(Unmute data, NetPeer peer)
        {
            if (Participants.TryGetValue(peer, out var client) && client.ClientSided)
            {
                if (!client.Binded) return;

                Broadcast(new Unmute() { Key = client.Key }, true, client);
            }
        }

        private void OnDeafen(Deafen data, NetPeer peer)
        {
            if (Participants.TryGetValue(peer, out var client) && client.ClientSided)
            {
                if (!client.Binded) return;

                Broadcast(new Deafen() { Key = client.Key }, true, client);
            }
        }

        private void OnUndeafen(Undeafen data, NetPeer peer)
        {
            if (Participants.TryGetValue(peer, out var client) && client.ClientSided)
            {
                if (!client.Binded) return;

                Broadcast(new Undeafen() { Key = client.Key }, true, client);
            }
        }

        private void OnJoinChannel(JoinChannel data, NetPeer peer)
        {
            var channel = ServerProperties.Channels.ElementAtOrDefault(data.ChannelId);
            if (channel != null)
            {

            }
            else
            {
                peer.AddToSendBuffer(new Deny() { Reason = "Invalid Channel Id" });
            }
        }

        private void OnLeaveChannel(LeaveChannel data, NetPeer peer)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}