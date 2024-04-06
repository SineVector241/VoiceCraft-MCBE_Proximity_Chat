using System.Collections.Concurrent;
using System.Net;
using System.Numerics;
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
            VoiceCraftSocket.OnUpdatePositionReceived += OnUpdatePosition;
            VoiceCraftSocket.OnClientAudioReceived += OnClientAudio;
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

        public void Broadcast(VoiceCraftPacket packet, VoiceCraftParticipant[] excludes, Channel? inChannel = null, bool bindedOnly = true)
        {
            var list = Participants.Where(x => !excludes.Contains(x.Value) && x.Value.Channel == inChannel);
            foreach (var participant in list)
            {
                if(participant.Value.Binded && bindedOnly || !bindedOnly)
                {
                    participant.Key.AddToSendBuffer(packet);
                }
            }
        }

        public void MoveParticipantToChannel(NetPeer peer, VoiceCraftParticipant client, Channel? channel = null)
        {
            if (client.Channel == channel) return; //Client is already in the channel.

            if(client.Channel != null)
                peer.AddToSendBuffer(new LeaveChannel()); //Tell the client to leave the previous channel

            Broadcast(new Core.Packets.VoiceCraft.ParticipantLeft() 
            { 
                Key = client.Key 
            }, [client], client.Channel);
            client.Channel = channel;

            if (client.Channel != null)
                peer.AddToSendBuffer(new JoinChannel() { ChannelId = (byte)ServerProperties.Channels.IndexOf(client.Channel)}); //Tell the client to join the channel

            Broadcast(new Core.Packets.VoiceCraft.ParticipantJoined()
            {
                IsDeafened = client.Deafened,
                IsMuted = client.Muted,
                Key = client.Key,
                Name = client.Name
            }, [client], client.Channel);

            var list = Participants.Where(x => x.Value != client && x.Value.Binded && x.Value.Channel == client.Channel);
            foreach (var participant in list)
            {
                peer.AddToSendBuffer(new Core.Packets.VoiceCraft.ParticipantJoined()
                {
                    IsDeafened = participant.Value.Deafened,
                    IsMuted = participant.Value.Muted,
                    Key = participant.Value.Key,
                    Name = participant.Value.Name
                });
            } //Send participants back to binded client.
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
            if(Participants.TryRemove(peer, out var client))
            {
                Broadcast(new Core.Packets.VoiceCraft.ParticipantLeft()
                { 
                    Key = client.Key 
                }, [], client.Channel); //Broadcast to all other participants.
                OnParticipantLeft?.Invoke(client, reason);
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
                }, [client], client.Channel); //Broadcast to all other participants.

                var list = Participants.Where(x => x.Value != client && x.Value.Binded && x.Value.Channel == client.Channel);
                foreach (var participant in list)
                {
                    peer.AddToSendBuffer(new Core.Packets.VoiceCraft.ParticipantJoined()
                    {
                        IsDeafened = participant.Value.Deafened,
                        IsMuted = participant.Value.Muted,
                        Key = participant.Value.Key,
                        Name = participant.Value.Name
                    });
                } //Send participants back to binded client.

                foreach(var channel in ServerProperties.Channels)
                {
                    peer.AddToSendBuffer(new AddChannel()
                    {
                        Name = channel.Name,
                        RequiresPassword = !string.IsNullOrWhiteSpace(channel.Password)
                    });
                } //Send channel list back to binded client.

                OnParticipantBinded?.Invoke(client);
            }
        }

        private void OnMute(Mute data, NetPeer peer)
        {
            if (Participants.TryGetValue(peer, out var client) && client.ClientSided)
            {
                if (!client.Binded) return;

                Broadcast(new Mute() { Key = client.Key }, [client], client.Channel);
            }
        }

        private void OnUnmute(Unmute data, NetPeer peer)
        {
            if (Participants.TryGetValue(peer, out var client) && client.ClientSided)
            {
                if (!client.Binded) return;

                Broadcast(new Unmute() { Key = client.Key }, [client], client.Channel);
            }
        }

        private void OnDeafen(Deafen data, NetPeer peer)
        {
            if (Participants.TryGetValue(peer, out var client) && client.ClientSided)
            {
                if (!client.Binded) return;

                Broadcast(new Deafen() { Key = client.Key }, [client], client.Channel);
            }
        }

        private void OnUndeafen(Undeafen data, NetPeer peer)
        {
            if (Participants.TryGetValue(peer, out var client) && client.ClientSided)
            {
                if (!client.Binded) return;

                Broadcast(new Undeafen() { Key = client.Key }, [client], client.Channel);
            }
        }

        private void OnJoinChannel(JoinChannel data, NetPeer peer)
        {
            var channel = ServerProperties.Channels.ElementAtOrDefault(data.ChannelId);
            if (Participants.TryGetValue(peer, out var client) && client.Binded && channel != null)
            {
                if(channel.Password == data.Password || string.IsNullOrWhiteSpace(channel.Password))
                {
                    peer.AddToSendBuffer(new Deny() { Reason = "Invalid Channel Password!" });
                    return;
                }
                MoveParticipantToChannel(peer, client, channel);
            }
        }

        private void OnLeaveChannel(LeaveChannel data, NetPeer peer)
        {
            if (Participants.TryGetValue(peer, out var client) && client.Binded && client.Channel != null)
            {
                MoveParticipantToChannel(peer, client, null);
            }
        }

        private void OnUpdatePosition(UpdatePosition data, NetPeer peer)
        {
            if(Participants.TryGetValue(peer, out var client) && client.Binded && client.ClientSided)
            {
                client.Position = data.Position;
                client.EnvironmentId = data.EnvironmentId;
            }
        }

        private void OnClientAudio(ClientAudio data, NetPeer peer)
        {
            _ = Task.Run(() => {
                if (Participants.TryGetValue(peer, out var client) && client.Binded && !client.Muted && !client.Deafened && !client.ServerMuted)
                {
                    client.LastSpoke = Environment.TickCount64;
                    var proximityToggle = client.Channel?.ChannelOverride?.ProximityToggle ?? ServerProperties.ProximityToggle;
                    if (proximityToggle)
                    {
                        if (client.Dead || string.IsNullOrWhiteSpace(client.EnvironmentId)) return;
                        var proximityDistance = client.Channel?.ChannelOverride?.ProximityDistance ?? ServerProperties.ProximityDistance;
                        var voiceEffects = client.Channel?.ChannelOverride?.VoiceEffects ?? ServerProperties.VoiceEffects;

                        var list = Participants.Where(x =>
                        x.Value != client &&
                        x.Value.Binded &&
                        !x.Value.Deafened &&
                        !x.Value.Dead &&
                        x.Value.Channel == client.Channel &&
                        !string.IsNullOrWhiteSpace(x.Value.EnvironmentId) &&
                        x.Value.EnvironmentId == client.EnvironmentId &&
                        Vector3.Distance(x.Value.Position, client.Position) <= proximityDistance); //Get Participants

                        for (ushort i = 0; i < list.Count(); i++)
                        {
                            var participant = list.ElementAt(i);
                            var volume = 1.0f - Math.Clamp(Vector3.Distance(participant.Value.Position, client.Position) / proximityDistance, 0.0f, 1.0f);
                            var echo = voiceEffects ? Math.Max(participant.Value.CaveDensity, client.CaveDensity) * (1.0f - volume) : 0.0f;
                            var muffled = voiceEffects && (participant.Value.InWater || client.InWater);
                            var rotation = (float)(Math.Atan2(participant.Value.Position.Z - client.Position.Z, participant.Value.Position.X - client.Position.X) - (participant.Value.Rotation * Math.PI / 180));

                            participant.Key.AddToSendBuffer(new ServerAudio()
                            { 
                                Key = client.Key, 
                                PacketCount = data.PacketCount, 
                                Volume = volume, 
                                EchoFactor = echo, 
                                Rotation = rotation, 
                                Muffled = muffled, 
                                Audio = data.Audio 
                            });
                        }
                    }
                    else
                    {
                        var list = Participants.Where(x =>
                        x.Value != client &&
                        x.Value.Binded &&
                        !x.Value.Deafened &&
                        x.Value.Channel == client.Channel);

                        for (ushort i = 0; i < list.Count(); i++)
                        {
                            var participant = list.ElementAt(i);
                            participant.Key.AddToSendBuffer(new ServerAudio()
                            {
                                Key = client.Key,
                                PacketCount = data.PacketCount,
                                Volume = 1.0f,
                                EchoFactor = 0.0f,
                                Rotation = 1.5f,
                                Muffled = false,
                                Audio = data.Audio
                            });
                        }
                    }
                }
            });
        }
        #endregion
    }
}