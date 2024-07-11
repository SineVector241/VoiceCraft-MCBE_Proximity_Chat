using System.Collections.Concurrent;
using System.Net;
using System.Numerics;
using VoiceCraft.Core;
using VoiceCraft.Core.Packets;
using VoiceCraft.Network;
using VoiceCraft.Server.Data;

namespace VoiceCraft.Server
{
    //Client - Requesting Participant
    //Participant - Receiving Participant
    public class VoiceCraftServer : Disposable
    {
        public const string Version = "1.0.5";
        public ConcurrentDictionary<NetPeer, VoiceCraftParticipant> Participants { get; set; } = new ConcurrentDictionary<NetPeer, VoiceCraftParticipant>();
        public Network.Sockets.VoiceCraft VoiceCraftSocket { get; set; }
        public Network.Sockets.MCComm MCComm { get; set; }
        public Properties ServerProperties { get; set; }
        public List<string> Banlist { get; set; }
        public bool IsStarted { get; private set; }

        #region Delegates
        public delegate void Started();
        public delegate void SocketStarted(Type socket);
        public delegate void Stopped(string? reason = null);
        public delegate void Failed(Exception ex);

        public delegate void ParticipantJoined(VoiceCraftParticipant participant);
        public delegate void ParticipantLeft(VoiceCraftParticipant participant, string? reason = null);
        public delegate void ParticipantBinded(VoiceCraftParticipant participant);
        #endregion

        #region Events
        public event Started? OnStarted;
        public event SocketStarted? OnSocketStarted;
        public event Stopped? OnStopped;
        public event Failed? OnFailed;

        public event ParticipantJoined? OnParticipantJoined;
        public event ParticipantLeft? OnParticipantLeft;
        public event ParticipantBinded? OnParticipantBinded;
        #endregion

        public VoiceCraftServer(Properties properties, List<string> banlist)
        {
            ServerProperties = properties;
            Banlist = banlist;
            VoiceCraftSocket = new Network.Sockets.VoiceCraft();
            MCComm = new Network.Sockets.MCComm();

            VoiceCraftSocket.OnStarted += VoiceCraftSocketStarted;
            VoiceCraftSocket.OnFailed += VoiceCraftSocketFailed;
            VoiceCraftSocket.OnStopped += VoiceCraftSocketStopped;
            VoiceCraftSocket.OnPingInfoReceived += OnPingInfo;
            VoiceCraftSocket.OnPeerConnected += OnPeerConnected;
            VoiceCraftSocket.OnPeerDisconnected += OnPeerDisconnected;
            VoiceCraftSocket.OnBindedReceived += OnBinded;
            VoiceCraftSocket.OnUnbindedReceived += VoiceCraftSocketUnbinded;
            VoiceCraftSocket.OnMuteReceived += OnMute;
            VoiceCraftSocket.OnUnmuteReceived += OnUnmute;
            VoiceCraftSocket.OnDeafenReceived += OnDeafen;
            VoiceCraftSocket.OnUndeafenReceived += OnUndeafen;
            VoiceCraftSocket.OnJoinChannelReceived += OnJoinChannel;
            VoiceCraftSocket.OnLeaveChannelReceived += OnLeaveChannel;
            VoiceCraftSocket.OnUpdatePositionReceived += OnUpdatePosition;
            VoiceCraftSocket.OnFullUpdatePositionReceived += OnFullUpdatePosition;
            VoiceCraftSocket.OnUpdateEnvironmentIdReceived += OnUpdateEnvironmentIdReceived;
            VoiceCraftSocket.OnClientAudioReceived += OnClientAudio;

            MCComm.OnStarted += MCCommStarted;
            MCComm.OnFailed += MCCommFailed;
            MCComm.OnBindReceived += MCCommBind;
            MCComm.OnUpdateReceived += MCCommUpdate;
            MCComm.OnGetChannelsReceived += MCCommGetChannels;
            MCComm.OnGetChannelSettingsReceived += MCCommGetChannelSettings;
            MCComm.OnSetChannelSettingsReceived += MCCommSetChannelSettings;
            MCComm.OnGetDefaultSettingsReceived += MCCommGetDefaultSettings;
            MCComm.OnSetDefaultSettingsReceived += MCCommSetDefaultSettings;
            MCComm.OnGetParticipantsReceived += MCCommGetParticipants;
            MCComm.OnDisconnectParticipantReceived += MCCommDisconnectParticipant;
            MCComm.OnGetParticipantBitmaskReceived += MCCommGetParticipantBitmask;
            MCComm.OnSetParticipantBitmaskReceived += MCCommSetParticipantBitmask;
            MCComm.OnMuteParticipantReceived += MCCommMuteParticipant;
            MCComm.OnUnmuteParticipantReceived += MCCommUnmuteParticipant;
            MCComm.OnDeafenParticipantReceived += MCCommDeafenParticipant;
            MCComm.OnUndeafenParticipantReceived += MCCommUndeafenParticipant;
            MCComm.OnANDModParticipantBitmaskReceived += MCCommANDModParticipantBitmask;
            MCComm.OnORModParticipantBitmaskReceived += MCCommORModParticipantBitmask;
            MCComm.OnXORModParticipantBitmaskReceived += MCCommXORModParticipantBitmask;
            MCComm.OnChannelMoveReceived += MCCommChannelMove;
        }

        #region Methods
        public void Start()
        {
            ObjectDisposedException.ThrowIf(IsDisposed, nameof(VoiceCraftServer));

            _ = Task.Run(async () => {
                try
                {
                    VoiceCraftSocket.LogExceptions = ServerProperties.Debugger.LogExceptions;
                    VoiceCraftSocket.LogInbound = ServerProperties.Debugger.LogInboundPackets;
                    VoiceCraftSocket.LogOutbound = ServerProperties.Debugger.LogOutboundPackets;
                    VoiceCraftSocket.InboundFilter = ServerProperties.Debugger.InboundPacketFilter;
                    VoiceCraftSocket.OutboundFilter = ServerProperties.Debugger.OutboundPacketFilter;
                    VoiceCraftSocket.Timeout = ServerProperties.ClientTimeoutMS;
                    await VoiceCraftSocket.HostAsync(ServerProperties.VoiceCraftPortUDP);
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    Stop(ex.Message);
                    OnFailed?.Invoke(ex);
                }
            });
        }

        public void Stop(string? reason = null)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, nameof(VoiceCraftServer));

            VoiceCraftSocket.StopAsync().Wait();
            MCComm.Stop();

            if (IsStarted)
            {
                IsStarted = false;
                OnStopped?.Invoke(reason);
            }
        }

        public void Broadcast(VoiceCraftPacket packet, VoiceCraftParticipant[]? excludes = null, Channel[]? inChannels = null)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, nameof(VoiceCraftServer));

            var list = Participants.Where(x => (excludes == null || !excludes.Contains(x.Value)) && (inChannels == null || inChannels.Contains(x.Value.Channel)));
            foreach (var participant in list)
            {
                participant.Key.AddToSendBuffer(packet.Clone());
            }
        }

        public void MoveParticipantToChannel(NetPeer peer, VoiceCraftParticipant client, Channel channel)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, nameof(VoiceCraftServer));

            if (client.Channel == channel) return; //Client is already in the channel, do nothing.

            //Tell the client to leave the previous channel/reset even if the channel is hidden.
            peer.AddToSendBuffer(new Core.Packets.VoiceCraft.LeaveChannel());

            //Tell the other clients that the participant has left the channel.
            Broadcast(new Core.Packets.VoiceCraft.ParticipantLeft()
            {
                Key = client.Key
            }, Participants.Values.Where(x => x == client || !x.Binded).ToArray(), [client.Channel]);

            //Set the client channel
            client.Channel = channel;

            //Tell the client it has joined the channel if it's not hidden.
            if (!channel.Hidden)
                peer.AddToSendBuffer(new Core.Packets.VoiceCraft.JoinChannel() { ChannelId = (byte)ServerProperties.Channels.IndexOf(channel) });

            //Tell the other clients in the channel to add the client.
            Broadcast(new Core.Packets.VoiceCraft.ParticipantJoined()
            {
                IsDeafened = client.Deafened,
                IsMuted = client.Muted,
                Key = client.Key,
                Name = client.Name
            }, Participants.Values.Where(x => x == client || !x.Binded).ToArray(), [client.Channel]);

            //Send new participants back to the client.
            foreach (var participant in Participants.Values.Where(x => x != client && x.Binded && x.Channel == client.Channel))
            {
                peer.AddToSendBuffer(new Core.Packets.VoiceCraft.ParticipantJoined()
                {
                    IsDeafened = participant.Deafened,
                    IsMuted = participant.Muted,
                    Key = participant.Key,
                    Name = participant.Name
                });
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
        private void VoiceCraftSocketStarted()
        {
            OnSocketStarted?.Invoke(typeof(Network.Sockets.VoiceCraft));

            if(ServerProperties.ConnectionType == ConnectionTypes.Client)
            {
                IsStarted = true;
                OnStarted?.Invoke();
                return;
            }

            _ = Task.Run(async () =>
            {
                try
                {
                    MCComm.LogExceptions = ServerProperties.Debugger.LogExceptions;
                    MCComm.LogInbound = ServerProperties.Debugger.LogInboundMCCommPackets;
                    MCComm.LogOutbound = ServerProperties.Debugger.LogOutboundMCCommPackets;
                    MCComm.InboundFilter = ServerProperties.Debugger.InboundMCCommFilter;
                    MCComm.OutboundFilter = ServerProperties.Debugger.OutboundMCCommFilter;
                    MCComm.Timeout = ServerProperties.ExternalServerTimeoutMS;
                    await MCComm.Start(ServerProperties.MCCommPortTCP, ServerProperties.PermanentServerKey);
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    Stop(ex.Message);
                    OnFailed?.Invoke(ex);
                }
            });
        }

        private void VoiceCraftSocketFailed(Exception ex)
        {
            OnFailed?.Invoke(ex);
        }

        private void VoiceCraftSocketStopped(string? reason = null)
        {
            Stop(reason);
        }

        private void OnPingInfo(Core.Packets.VoiceCraft.PingInfo data, NetPeer peer)
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
            peer.AddToSendBuffer(new Core.Packets.VoiceCraft.PingInfo() { ConnectedParticipants = Participants.Count, MOTD = ServerProperties.ServerMOTD, PositioningType = connType});
            peer.Disconnect(notify: false);
        }

        private void OnPeerConnected(NetPeer peer, Core.Packets.VoiceCraft.Login packet)
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
            var participant = new VoiceCraftParticipant(string.Empty, ServerProperties.DefaultChannel)
            {
                ClientSided = PositioningTypes.ClientSided == packet.PositioningType,
                Key = GetAvailableKey(packet.Key)
            };
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
                }, Participants.Values.Where(x => !x.Binded).ToArray(), [client.Channel]); //Broadcast to all other participants.
                OnParticipantLeft?.Invoke(client, reason);
            }
        }

        private void OnBinded(Core.Packets.VoiceCraft.Binded data, NetPeer peer)
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
                }, Participants.Values.Where(x => x == client || !x.Binded).ToArray(), [client.Channel]); //Broadcast to all other participants.

                foreach (var participant in Participants.Values.Where(x => x != client && x.Binded && x.Channel == client.Channel))
                {
                    peer.AddToSendBuffer(new Core.Packets.VoiceCraft.ParticipantJoined()
                    {
                        IsDeafened = participant.Deafened,
                        IsMuted = participant.Muted,
                        Key = participant.Key,
                        Name = participant.Name
                    });
                } //Send participants back to binded client.

                byte channelId = 0;
                foreach(var channel in ServerProperties.Channels)
                {
                    if(channel.Hidden)
                    {
                        channelId++;
                        continue; //Do not send a hidden channel.
                    }

                    peer.AddToSendBuffer(new Core.Packets.VoiceCraft.AddChannel()
                    {
                        Name = channel.Name,
                        Locked = channel.Locked,
                        RequiresPassword = !string.IsNullOrWhiteSpace(channel.Password),
                        ChannelId = channelId
                    });
                    channelId++;
                } //Send channel list back to binded client.

                if(!client.Channel.Hidden)
                    peer.AddToSendBuffer(new Core.Packets.VoiceCraft.JoinChannel() { ChannelId = (byte)ServerProperties.Channels.IndexOf(client.Channel) }); //Tell the client that it is in a channel.

                OnParticipantBinded?.Invoke(client);
            }
        }

        private void VoiceCraftSocketUnbinded(Core.Packets.VoiceCraft.Unbinded data, NetPeer peer)
        {
            if (Participants.TryGetValue(peer, out var client) && client.ClientSided && client.Binded)
            {
                client.Binded = false;
                Broadcast(new Core.Packets.VoiceCraft.ParticipantLeft() { Key = client.Key }, Participants.Values.Where(x => x == client || !x.Binded).ToArray(), [client.Channel]);
                OnParticipantLeft?.Invoke(client, "Unbinded.");
            }
        }

        private void OnMute(Core.Packets.VoiceCraft.Mute data, NetPeer peer)
        {
            if (Participants.TryGetValue(peer, out var client) && !client.Muted)
            {
                client.Muted = true;
                Broadcast(new Core.Packets.VoiceCraft.Mute() { Key = client.Key }, Participants.Values.Where(x => x == client || !x.Binded).ToArray(), [client.Channel]);
            }
        }

        private void OnUnmute(Core.Packets.VoiceCraft.Unmute data, NetPeer peer)
        {
            if (Participants.TryGetValue(peer, out var client) && client.Muted)
            {
                client.Muted = false;
                Broadcast(new Core.Packets.VoiceCraft.Unmute() { Key = client.Key }, Participants.Values.Where(x => x == client || !x.Binded).ToArray(), [client.Channel]);
            }
        }

        private void OnDeafen(Core.Packets.VoiceCraft.Deafen data, NetPeer peer)
        {
            if (Participants.TryGetValue(peer, out var client) && !client.Deafened)
            {
                client.Deafened = true;
                Broadcast(new Core.Packets.VoiceCraft.Deafen() { Key = client.Key }, Participants.Values.Where(x => x == client || !x.Binded).ToArray(), [client.Channel]);
            }
        }

        private void OnUndeafen(Core.Packets.VoiceCraft.Undeafen data, NetPeer peer)
        {
            if (Participants.TryGetValue(peer, out var client) && client.Deafened)
            {
                client.Deafened = false;
                Broadcast(new Core.Packets.VoiceCraft.Undeafen() { Key = client.Key }, Participants.Values.Where(x => x == client || !x.Binded).ToArray(), [client.Channel]);
            }
        }

        private void OnJoinChannel(Core.Packets.VoiceCraft.JoinChannel data, NetPeer peer)
        {
            if (data.ChannelId < ServerProperties.Channels.Count && Participants.TryGetValue(peer, out var client) && client.Binded)
            {
                var channel = ServerProperties.Channels[data.ChannelId];
                if (channel.Locked) return; //Locked Channel, Do Nothing.

                if(channel.Password != data.Password && !string.IsNullOrWhiteSpace(channel.Password))
                {
                    peer.AddToSendBuffer(new Core.Packets.VoiceCraft.Deny() { Reason = "Invalid Channel Password!" });
                    return;
                }
                MoveParticipantToChannel(peer, client, channel);
            }
        }

        private void OnLeaveChannel(Core.Packets.VoiceCraft.LeaveChannel data, NetPeer peer)
        {
            if (Participants.TryGetValue(peer, out var client) && client.Binded)
            {
                MoveParticipantToChannel(peer, client, ServerProperties.DefaultChannel);
            }
        }

        private void OnUpdatePosition(Core.Packets.VoiceCraft.UpdatePosition data, NetPeer peer)
        {
            if(Participants.TryGetValue(peer, out var client) && client.Binded && client.ClientSided)
            {
                client.Position = data.Position;
            }
        }

        private void OnFullUpdatePosition(Core.Packets.VoiceCraft.FullUpdatePosition data, NetPeer peer)
        {
            if (Participants.TryGetValue(peer, out var client) && client.Binded && client.ClientSided)
            {
                client.Position = data.Position;
                client.Rotation = data.Rotation;
                client.CaveDensity = data.CaveDensity;
                client.Dead = data.IsDead;
                client.InWater = data.InWater;
            }
        }

        private void OnUpdateEnvironmentIdReceived(Core.Packets.VoiceCraft.UpdateEnvironmentId data, NetPeer peer)
        {
            if (Participants.TryGetValue(peer, out var client) && client.Binded && client.ClientSided)
            {
                client.EnvironmentId = data.EnvironmentId;
            }
        }

        private void OnClientAudio(Core.Packets.VoiceCraft.ClientAudio data, NetPeer peer)
        {
            _ = Task.Run(() => {
                if (Participants.TryGetValue(peer, out var client) && client.Binded && !client.Muted && !client.Deafened && !client.ServerMuted && !client.ServerDeafened)
                {
                    client.LastSpoke = Environment.TickCount64;
                    var defaultSettings = ServerProperties.DefaultSettings;
                    var proximityToggle = client.Channel.OverrideSettings?.ProximityToggle ?? defaultSettings.ProximityToggle;
                    if (proximityToggle)
                    {
                        if (!VoiceCraftParticipant.TalkSettingsDisabled(client.ChecksBitmask, BitmaskSettings.DeathDisabled) && client.Dead || 
                            !VoiceCraftParticipant.TalkSettingsDisabled(client.ChecksBitmask, BitmaskSettings.EnvironmentDisabled) && string.IsNullOrWhiteSpace(client.EnvironmentId)) 
                            return;

                        var proximityDistance = client.Channel.OverrideSettings?.ProximityDistance ?? defaultSettings.ProximityDistance;
                        var voiceEffects = client.Channel.OverrideSettings?.VoiceEffects ?? defaultSettings.VoiceEffects;

                        var list = Participants.Where(x =>
                        x.Value != client &&
                        x.Value.Binded &&
                        !x.Value.Deafened &&
                        !x.Value.ServerDeafened &&
                        x.Value.Channel == client.Channel &&

                        //Bitmask Checks here
                        (x.Value.GetIntersectedListenBitmasks(client.ChecksBitmask) != 0) && //Matching Bitmasks
                        (x.Value.IntersectedListenSettingsDisabled(client.ChecksBitmask, BitmaskSettings.DeathDisabled) || !x.Value.Dead) &&
                        (x.Value.IntersectedListenSettingsDisabled(client.ChecksBitmask, BitmaskSettings.EnvironmentDisabled) || (!string.IsNullOrWhiteSpace(x.Value.EnvironmentId) && x.Value.EnvironmentId == client.EnvironmentId)) &&
                        (x.Value.IntersectedListenSettingsDisabled(client.ChecksBitmask, BitmaskSettings.ProximityDisabled) || Vector3.Distance(x.Value.Position, client.Position) <= proximityDistance)
                        ); //Get Participants

                        foreach(var participant in list)
                        {
                            var volume = participant.Value.IntersectedListenSettingsDisabled(client.ChecksBitmask, BitmaskSettings.ProximityDisabled) ? 1.0f : 1.0f - Math.Clamp(Vector3.Distance(participant.Value.Position, client.Position) / proximityDistance, 0.0f, 1.0f);
                            var echo = participant.Value.IntersectedListenSettingsDisabled(client.ChecksBitmask, BitmaskSettings.VoiceEffectsDisabled) || !voiceEffects ? 0.0f : Math.Max(participant.Value.CaveDensity, client.CaveDensity) * (1.0f - volume);
                            var muffled = !participant.Value.IntersectedListenSettingsDisabled(client.ChecksBitmask, BitmaskSettings.VoiceEffectsDisabled) && voiceEffects && (participant.Value.InWater || client.InWater);
                            var rotation = participant.Value.IntersectedListenSettingsDisabled(client.ChecksBitmask, BitmaskSettings.ProximityDisabled)? 1.5f : (float)(Math.Atan2(participant.Value.Position.Z - client.Position.Z, participant.Value.Position.X - client.Position.X) - (participant.Value.Rotation * Math.PI / 180));

                            participant.Key.AddToSendBuffer(new Core.Packets.VoiceCraft.ServerAudio()
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
                        x.Value.Channel == client.Channel &&
                        (((x.Value.ChecksBitmask >> 6) & (client.ChecksBitmask >> 11)) != 0));

                        for (ushort i = 0; i < list.Count(); i++)
                        {
                            var participant = list.ElementAt(i);
                            participant.Key.AddToSendBuffer(new Core.Packets.VoiceCraft.ServerAudio()
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

        #region MCComm Event Methods
        private void MCCommStarted()
        {
            OnSocketStarted?.Invoke(typeof(Network.Sockets.MCComm));

            IsStarted = true;
            OnStarted?.Invoke();
        }

        private void MCCommFailed(Exception ex)
        {
            OnFailed?.Invoke(ex);
        }

        private void MCCommBind(Core.Packets.MCComm.Bind packet, HttpListenerContext ctx)
        {
            var client = Participants.FirstOrDefault(x => x.Value.Key == packet.PlayerKey);
            if (client.Value == null)
            {
                MCComm.SendResponse(ctx, HttpStatusCode.OK, new Core.Packets.MCComm.Deny() { Reason = "Could not find key!" });
                return;
            }
            if (client.Value.Binded)
            {
                MCComm.SendResponse(ctx, HttpStatusCode.OK, new Core.Packets.MCComm.Deny() { Reason = "Key has already been binded to a participant!" });
                return;
            }
            if (Participants.FirstOrDefault(x => x.Value.MinecraftId == packet.PlayerId).Value != null)
            {
                MCComm.SendResponse(ctx, HttpStatusCode.OK, new Core.Packets.MCComm.Deny() { Reason = "PlayerId is already binded to a participant!" });
                return;
            }
            if (client.Value.ClientSided)
            {
                MCComm.SendResponse(ctx, HttpStatusCode.OK, new Core.Packets.MCComm.Deny() { Reason = "Participant is using client sided positioning!" });
                return;
            }

            client.Value.Name = packet.Gamertag;
            client.Value.MinecraftId = packet.PlayerId;
            client.Value.Binded = true;
            client.Key.AddToSendBuffer(new Core.Packets.VoiceCraft.Binded() { Name = client.Value.Name });

            MCComm.SendResponse(ctx, HttpStatusCode.OK, new Core.Packets.MCComm.Accept());

            Broadcast(new Core.Packets.VoiceCraft.ParticipantJoined()
            {
                IsDeafened = client.Value.Deafened,
                IsMuted = client.Value.Muted,
                Key = client.Value.Key,
                Name = client.Value.Name
            }, Participants.Values.Where(x => x == client.Value || !x.Binded).ToArray(), [client.Value.Channel]); //Broadcast to all other participants.

            var list = Participants.Where(x => x.Value != client.Value && x.Value.Binded && x.Value.Channel == client.Value.Channel);
            foreach (var participant in list)
            {
                client.Key.AddToSendBuffer(new Core.Packets.VoiceCraft.ParticipantJoined()
                {
                    IsDeafened = participant.Value.Deafened,
                    IsMuted = participant.Value.Muted,
                    Key = participant.Value.Key,
                    Name = participant.Value.Name
                });
            } //Send participants back to binded client.

            byte channelId = 0;
            foreach (var channel in ServerProperties.Channels)
            {
                if (channel.Hidden)
                {
                    channelId++;
                    continue; //Do not send a hidden channel.
                }

                client.Key.AddToSendBuffer(new Core.Packets.VoiceCraft.AddChannel()
                {
                    Name = channel.Name,
                    Locked = channel.Locked,
                    RequiresPassword = !string.IsNullOrWhiteSpace(channel.Password),
                    ChannelId = channelId
                });
                channelId++;
            } //Send channel list back to binded client.

            if (!client.Value.Channel.Hidden)
                client.Key.AddToSendBuffer(new Core.Packets.VoiceCraft.JoinChannel() { ChannelId = (byte)ServerProperties.Channels.IndexOf(client.Value.Channel)}); //Tell the client that it is in a channel.

            OnParticipantBinded?.Invoke(client.Value);
        }

        private void MCCommUpdate(Core.Packets.MCComm.Update packet, HttpListenerContext ctx)
        {
            for (int i = 0; i < packet.Players.Count; i++)
            {
                var player = packet.Players[i];
                var participant = Participants.FirstOrDefault(x => x.Value.MinecraftId == player.PlayerId && !x.Value.ClientSided);
                if (participant.Value != null)
                {
                    participant.Value.Position = player.Location;
                    participant.Value.EnvironmentId = player.DimensionId;
                    participant.Value.Rotation = player.Rotation;
                    participant.Value.CaveDensity = player.CaveDensity;
                    participant.Value.Dead = player.IsDead;
                    participant.Value.InWater = player.InWater;
                }
            }

            MCComm.SendResponse(ctx, HttpStatusCode.OK, new Core.Packets.MCComm.AckUpdate() { SpeakingPlayers = Participants.Values.Where(x => Environment.TickCount64 - x.LastSpoke <= 500).Select(x => x.MinecraftId).ToList() });
        }

        private void MCCommGetChannels(Core.Packets.MCComm.GetChannels packet, HttpListenerContext ctx)
        {
            packet.Channels.Clear();
            for (byte i = 0; i < ServerProperties.Channels.Count; i++)
            {
                packet.Channels.Add(i, ServerProperties.Channels[i]);
            }
            packet.Token = string.Empty;
            MCComm.SendResponse(ctx, HttpStatusCode.OK, packet);
        }

        private void MCCommGetChannelSettings(Core.Packets.MCComm.GetChannelSettings packet, HttpListenerContext ctx)
        {
            if(packet.ChannelId >= ServerProperties.Channels.Count)
            {
                MCComm.SendResponse(ctx, HttpStatusCode.OK, new Core.Packets.MCComm.Deny() { Reason = "Channel does not exist!" });
                return;
            }

            var channel = ServerProperties.Channels[packet.ChannelId];
            packet.ProximityDistance = channel.OverrideSettings?.ProximityDistance ?? ServerProperties.DefaultSettings.ProximityDistance;
            packet.ProximityToggle = channel.OverrideSettings?.ProximityToggle ?? ServerProperties.DefaultSettings.ProximityToggle;
            packet.VoiceEffects = channel.OverrideSettings?.VoiceEffects ?? ServerProperties.DefaultSettings.VoiceEffects;
            packet.Token = string.Empty;

            MCComm.SendResponse(ctx, HttpStatusCode.OK, packet);
        }

        private void MCCommSetChannelSettings(Core.Packets.MCComm.SetChannelSettings packet, HttpListenerContext ctx)
        {
            if (packet.ChannelId >= ServerProperties.Channels.Count)
            {
                MCComm.SendResponse(ctx, HttpStatusCode.OK, new Core.Packets.MCComm.Deny() { Reason = "Channel does not exist!" });
                return;
            }

            var channel = ServerProperties.Channels[packet.ChannelId];
            if (packet.ProximityDistance < 1 || packet.ProximityDistance > 120)
            {
                MCComm.SendResponse(ctx, HttpStatusCode.OK, new Core.Packets.MCComm.Deny() { Reason = "Proximity distance must be between 1 and 120!" });
                return;
            }

            if (channel.OverrideSettings == null)
                channel.OverrideSettings = new ChannelOverride();

            channel.OverrideSettings.ProximityDistance = packet.ProximityDistance;
            channel.OverrideSettings.ProximityToggle = packet.ProximityToggle;
            channel.OverrideSettings.VoiceEffects = packet.VoiceEffects;
            MCComm.SendResponse(ctx, HttpStatusCode.OK, new Core.Packets.MCComm.Accept());
        }

        private void MCCommGetDefaultSettings(Core.Packets.MCComm.GetDefaultSettings packet, HttpListenerContext ctx)
        {
            packet.ProximityDistance = ServerProperties.DefaultSettings.ProximityDistance;
            packet.ProximityToggle = ServerProperties.DefaultSettings.ProximityToggle;
            packet.VoiceEffects = ServerProperties.DefaultSettings.VoiceEffects;
            packet.Token = string.Empty;
            MCComm.SendResponse(ctx, HttpStatusCode.OK, packet);
        }

        private void MCCommSetDefaultSettings(Core.Packets.MCComm.SetDefaultSettings packet, HttpListenerContext ctx)
        {
            if (packet.ProximityDistance < 1 || packet.ProximityDistance > 120)
            {
                MCComm.SendResponse(ctx, HttpStatusCode.OK, new Core.Packets.MCComm.Deny() { Reason = "Proximity distance must be between 1 and 120!" });
                return;
            }

            ServerProperties.DefaultSettings.ProximityDistance = packet.ProximityDistance;
            ServerProperties.DefaultSettings.ProximityToggle = packet.ProximityToggle;
            ServerProperties.DefaultSettings.VoiceEffects = packet.VoiceEffects;

            MCComm.SendResponse(ctx, HttpStatusCode.OK, new Core.Packets.MCComm.Accept());
        }

        private void MCCommGetParticipants(Core.Packets.MCComm.GetParticipants packet, HttpListenerContext ctx)
        {
            packet.Players = Participants.Values.Where(x => x.Binded).Select(x => x.MinecraftId).ToList();
            packet.Token = string.Empty;
            MCComm.SendResponse(ctx, HttpStatusCode.OK, packet);
        }

        private void MCCommDisconnectParticipant(Core.Packets.MCComm.DisconnectParticipant packet, HttpListenerContext ctx)
        {
            var participant = Participants.FirstOrDefault(x => x.Value.MinecraftId == packet.PlayerId);
            if (participant.Value != null)
            {
                participant.Key.Disconnect("MCComm server kicked.", true);
                MCComm.SendResponse(ctx, HttpStatusCode.OK, new Core.Packets.MCComm.Accept());
                return;
            }

            MCComm.SendResponse(ctx, HttpStatusCode.OK, new Core.Packets.MCComm.Deny() { Reason = "Could not find participant!" });
        }

        private void MCCommGetParticipantBitmask(Core.Packets.MCComm.GetParticipantBitmask packet, HttpListenerContext ctx)
        {
            var client = Participants.FirstOrDefault(x => x.Value.MinecraftId == packet.PlayerId);
            if (client.Value != null)
            {
                packet.Bitmask = client.Value.ChecksBitmask;
                packet.Token = string.Empty;
                MCComm.SendResponse(ctx, HttpStatusCode.OK, packet);
                return;
            }

            MCComm.SendResponse(ctx, HttpStatusCode.OK, new Core.Packets.MCComm.Deny() { Reason = "Could not find participant!" });
        }

        private void MCCommSetParticipantBitmask(Core.Packets.MCComm.SetParticipantBitmask packet, HttpListenerContext ctx)
        {
            var client = Participants.FirstOrDefault(x => x.Value.MinecraftId == packet.PlayerId);
            if (client.Value != null)
            {
                client.Value.ChecksBitmask = packet.Bitmask;
                MCComm.SendResponse(ctx, HttpStatusCode.OK, new Core.Packets.MCComm.Accept());
                return;
            }

            MCComm.SendResponse(ctx, HttpStatusCode.OK, new Core.Packets.MCComm.Deny() { Reason = "Could not find participant!" });
        }

        private void MCCommMuteParticipant(Core.Packets.MCComm.MuteParticipant packet, HttpListenerContext ctx)
        {
            var client = Participants.FirstOrDefault(x => x.Value.MinecraftId == packet.PlayerId);
            if (client.Value != null)
            {
                client.Value.ServerMuted = true;
                MCComm.SendResponse(ctx, HttpStatusCode.OK, new Core.Packets.MCComm.Accept());
                return;
            }

            MCComm.SendResponse(ctx, HttpStatusCode.OK, new Core.Packets.MCComm.Deny() { Reason = "Could not find participant!" });
        }

        private void MCCommUnmuteParticipant(Core.Packets.MCComm.UnmuteParticipant packet, HttpListenerContext ctx)
        {
            var client = Participants.FirstOrDefault(x => x.Value.MinecraftId == packet.PlayerId);
            if (client.Value != null)
            {
                client.Value.ServerMuted = false;
                MCComm.SendResponse(ctx, HttpStatusCode.OK, new Core.Packets.MCComm.Accept());
                return;
            }

            MCComm.SendResponse(ctx, HttpStatusCode.OK, new Core.Packets.MCComm.Deny() { Reason = "Could not find participant!" });
        }

        private void MCCommDeafenParticipant(Core.Packets.MCComm.DeafenParticipant packet, HttpListenerContext ctx)
        {
            var client = Participants.FirstOrDefault(x => x.Value.MinecraftId == packet.PlayerId);
            if (client.Value != null)
            {
                client.Value.ServerDeafened = true;
                MCComm.SendResponse(ctx, HttpStatusCode.OK, new Core.Packets.MCComm.Accept());
                return;
            }

            MCComm.SendResponse(ctx, HttpStatusCode.OK, new Core.Packets.MCComm.Deny() { Reason = "Could not find participant!" });
        }

        private void MCCommUndeafenParticipant(Core.Packets.MCComm.UndeafenParticipant packet, HttpListenerContext ctx)
        {
            var client = Participants.FirstOrDefault(x => x.Value.MinecraftId == packet.PlayerId);
            if (client.Value != null)
            {
                client.Value.ServerDeafened = false;
                MCComm.SendResponse(ctx, HttpStatusCode.OK, new Core.Packets.MCComm.Accept());
                return;
            }

            MCComm.SendResponse(ctx, HttpStatusCode.OK, new Core.Packets.MCComm.Deny() { Reason = "Could not find participant!" });
        }

        private void MCCommANDModParticipantBitmask(Core.Packets.MCComm.ANDModParticipantBitmask packet, HttpListenerContext ctx)
        {
            var client = Participants.FirstOrDefault(x => x.Value.MinecraftId == packet.PlayerId);
            if (client.Value != null)
            {
                client.Value.ChecksBitmask &= packet.Bitmask;
                MCComm.SendResponse(ctx, HttpStatusCode.OK, new Core.Packets.MCComm.Accept());
                return;
            }

            MCComm.SendResponse(ctx, HttpStatusCode.OK, new Core.Packets.MCComm.Deny() { Reason = "Could not find participant!" });
        }

        private void MCCommORModParticipantBitmask(Core.Packets.MCComm.ORModParticipantBitmask packet, HttpListenerContext ctx)
        {
            var client = Participants.FirstOrDefault(x => x.Value.MinecraftId == packet.PlayerId);
            if (client.Value != null)
            {
                client.Value.ChecksBitmask |= packet.Bitmask;
                MCComm.SendResponse(ctx, HttpStatusCode.OK, new Core.Packets.MCComm.Accept());
                return;
            }

            MCComm.SendResponse(ctx, HttpStatusCode.OK, new Core.Packets.MCComm.Deny() { Reason = "Could not find participant!" });
        }

        private void MCCommXORModParticipantBitmask(Core.Packets.MCComm.XORModParticipantBitmask packet, HttpListenerContext ctx)
        {
            var client = Participants.FirstOrDefault(x => x.Value.MinecraftId == packet.PlayerId);
            if (client.Value != null)
            {
                client.Value.ChecksBitmask ^= packet.Bitmask;
                MCComm.SendResponse(ctx, HttpStatusCode.OK, new Core.Packets.MCComm.Accept());
                return;
            }

            MCComm.SendResponse(ctx, HttpStatusCode.OK, new Core.Packets.MCComm.Deny() { Reason = "Could not find participant!" });
        }

        private void MCCommChannelMove(Core.Packets.MCComm.ChannelMove packet, HttpListenerContext ctx)
        {
            var client = Participants.FirstOrDefault(x => x.Value.MinecraftId == packet.PlayerId);

            if (packet.ChannelId >= ServerProperties.Channels.Count)
            {
                MCComm.SendResponse(ctx, HttpStatusCode.OK, new Core.Packets.MCComm.Deny() { Reason = "Channel does not exist!" });
                return;
            }
            if (client.Value == null)
            {
                MCComm.SendResponse(ctx, HttpStatusCode.OK, new Core.Packets.MCComm.Deny() { Reason = "Could not find participant!" });
                return;
            }

            var channel = ServerProperties.Channels[packet.ChannelId];
            if (channel == client.Value.Channel)
            {
                MCComm.SendResponse(ctx, HttpStatusCode.OK, new Core.Packets.MCComm.Deny() { Reason = "Participant is already in the channel!" });
                return;
            }

            MoveParticipantToChannel(client.Key, client.Value, channel);
            MCComm.SendResponse(ctx, HttpStatusCode.OK, new Core.Packets.MCComm.Accept());
        }
        #endregion

        protected override void Dispose(bool disposing)
        {
            if(disposing)
            {
                VoiceCraftSocket.Dispose();
                MCComm.Dispose();
            }
        }
    }
}