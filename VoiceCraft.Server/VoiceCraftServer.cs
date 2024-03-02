using Fleck;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using VoiceCraft.Core.Packets;
using VoiceCraft.Core.Sockets;
using VoiceCraft.Data.Server;

namespace VoiceCraft.Core.Server
{
    public class VoiceCraftServer : IDisposable
    {
        //Constants
        public const string Version = "v1.0.2";
        public const int ActivityInterval = 1000;

        //Data
        public ConcurrentDictionary<ushort, VoiceCraftParticipant> Participants { get; set; } = new ConcurrentDictionary<ushort, VoiceCraftParticipant>();
        public List<ExternalServer> ExternalServers { get; set; } = new List<ExternalServer>();
        public ServerState ServerState { get; set; } = ServerState.Stopped;

        public Properties ServerProperties { get; set; } = new Properties();
        public Banlist Banlist { get; set; } = new Banlist();
        private CancellationTokenSource CTS { get; } = new CancellationTokenSource();
        private Task? ActivityChecker { get; set; }
        public bool IsDisposed { get; private set; }

        //Sockets
        public Signalling Signalling { get; set; }
        public Voice Voice { get; set; }
        public MCCOMM MCComm { get; set; }

        //Delegates
        public delegate void SignallingStarted();
        public delegate void VoiceStarted();
        public delegate void WebserverStarted();
        public delegate void ExternalServerConnected(ExternalServer server);
        public delegate void ExternalServerDisconnected(ExternalServer server, string reason);
        public delegate void ParticipantConnected(VoiceCraftParticipant participant, ushort key);
        public delegate void ParticipantBinded(VoiceCraftParticipant participant, ushort key);
        public delegate void ParticipantUnbinded(VoiceCraftParticipant participant, ushort key);
        public delegate void ParticipantDisconnected(string reason, VoiceCraftParticipant participant, ushort key);
        public delegate void ExceptionError(Exception exception);
        public delegate void Error(Exception exception);

        //Events
        public event SignallingStarted? OnSignallingStarted;
        public event VoiceStarted? OnVoiceStarted;
        public event WebserverStarted? OnWebserverStarted;
        public event ExternalServerConnected? OnExternalServerConnected;
        public event ExternalServerDisconnected? OnExternalServerDisconnected;
        public event ParticipantConnected? OnParticipantConnected;
        public event ParticipantBinded? OnParticipantBinded;
        public event ParticipantUnbinded? OnParticipantUnbinded;
        public event ParticipantDisconnected? OnParticipantDisconnected;
        public event ExceptionError? OnExceptionError;
        public event Error? OnError;

        public VoiceCraftServer()
        {
            Signalling = new Signalling();
            Voice = new Voice();
            MCComm = new MCCOMM(CTS.Token);

            //Event methods in order!
            //Signalling
            Signalling.OnLogin += Signalling_Login; ;
            Signalling.OnBindedUnbinded += Signalling_BindedUnbinded;
            Signalling.OnMuteUnmute += Signalling_MuteUnmute;
            Signalling.OnDeafenUndeafen += Signalling_DeafenUndeafen;
            Signalling.OnJoinLeaveChannel += Signalling_JoinLeaveChannel;
            Signalling.OnSocketConnected += Signalling_SocketConnected;
            Signalling.OnSocketDisconnected += Signalling_SocketDisconnected;
            Signalling.OnPingCheck += Signalling_PingCheck;
            Signalling.OnPing += Signalling_Ping;

            //Voice
            Voice.OnLogin += Voice_Login;
            Voice.OnKeepAlive += Voice_KeepAlive;
            Voice.OnClientAudio += Voice_ClientAudio;
            Voice.OnUpdatePosition += Voice_UpdatePosition;

            //MCComm
            MCComm.OnLoginPacketReceived += MCCommLogin;
            MCComm.OnBindedPacketReceived += MCCommBinded;
            MCComm.OnUpdatePacketReceived += MCCommUpdate;
            MCComm.OnGetSettingsPacketReceived += MCCommGetSettings;
            MCComm.OnUpdateSettingsPacketReceived += MCCommUpdateSettings;
            MCComm.OnRemoveParticipantPacketReceived += MCCommRemoveParticipant;
            MCComm.OnChannelMovePacketReceived += MCCommChannelMove;
        }

        public void Start()
        {
            if (ServerState != ServerState.Stopped) throw new Exception("Server already running!");

            ServerState = ServerState.Starting;
            ActivityChecker = Task.Run(ServerChecks);
            try
            {
                //Host Signalling
                Signalling.LogInbound = ServerProperties.Debugger.LogInboundSignallingPackets;
                Signalling.LogOutbound = ServerProperties.Debugger.LogOutboundSignallingPackets;
                Signalling.InboundFilter = ServerProperties.Debugger.InboundSignallingFilter;
                Signalling.OutboundFilter = ServerProperties.Debugger.OutboundSignallingFilter;
                Signalling.LogExceptions = ServerProperties.Debugger.LogExceptions;
                Signalling.Host(ServerProperties.SignallingPortTCP);
                OnSignallingStarted?.Invoke();

                //Host Voice
                Voice.LogInbound = ServerProperties.Debugger.LogInboundVoicePackets;
                Voice.LogOutbound = ServerProperties.Debugger.LogOutboundVoicePackets;
                Voice.InboundFilter = ServerProperties.Debugger.InboundVoiceFilter;
                Voice.OutboundFilter = ServerProperties.Debugger.OutboundVoiceFilter;
                Voice.LogExceptions = ServerProperties.Debugger.LogExceptions;
                Voice.Host(ServerProperties.VoicePortUDP);
                OnVoiceStarted?.Invoke();

                if (ServerProperties.ConnectionType == ConnectionTypes.Server || ServerProperties.ConnectionType == ConnectionTypes.Hybrid)
                {
                    MCComm.LogInbound = ServerProperties.Debugger.LogInboundMCCommPackets;
                    MCComm.LogOutbound = ServerProperties.Debugger.LogOutboundMCCommPackets;
                    MCComm.InboundFilter = ServerProperties.Debugger.InboundMCCommFilter;
                    MCComm.OutboundFilter = ServerProperties.Debugger.OutboundMCCommFilter;
                    MCComm.LogExceptions = ServerProperties.Debugger.LogExceptions;
                    MCComm.Start(ServerProperties.MCCommPortTCP, ServerProperties.PermanentServerKey);
                    OnWebserverStarted?.Invoke();

                    /*
                        var username = Environment.GetEnvironmentVariable("USERNAME");
                        var userdomain = Environment.GetEnvironmentVariable("USERDOMAIN");
                        Console.WriteLine($"Please give access by typing in the following command in a command prompt\nnetsh http add urlacl url=http://*:{Port}/ user={userdomain}\\{username} listen=yes\nAnd then restart the server\n");
                        */
                }

                ServerState = ServerState.Started;
            }
            catch (Exception ex)
            {
                OnError?.Invoke(ex);
                CTS.Cancel();
                ServerState = ServerState.Stopped;
            }
        }

        //Event Methods
        #region Signalling
        private void Signalling_Ping(Packets.Signalling.Ping data, Socket socket)
        {
            var connType = "Hybrid";

            switch (ServerProperties.ConnectionType)
            {
                case ConnectionTypes.Server:
                    connType = "Server";
                    break;
                case ConnectionTypes.Client:
                    connType = "Client";
                    break;
            }

            Signalling.SendPacket(Packets.Signalling.Ping.Create(
                    $"MOTD: {ServerProperties.ServerMOTD}" +
                    $"\nConnection Type: {connType}" +
                    $"\nConnected Participants: {Participants.Count}"), socket);
        }

        private void Signalling_Login(Packets.Signalling.Login data, Socket socket)
        {
            if (Version != data.Version)
            {
                _ = Signalling.SendPacketAsync(Packets.Signalling.Deny.Create("Versions do not match!"), socket);
                return;
            }
            if (Banlist.IPBans.Exists(x => x == socket.RemoteEndPoint?.ToString()?.Split(':').FirstOrDefault()))
            {
                _ = Signalling.SendPacketAsync(Packets.Signalling.Deny.Create("You have been banned from the server!"), socket);
                return;
            }
            if (data.PositioningType != PositioningTypes.ClientSided &&
                (ServerProperties.ConnectionType == ConnectionTypes.Client || ServerProperties.ConnectionType == ConnectionTypes.Hybrid))
            {
                _ = Signalling.SendPacketAsync(Packets.Signalling.Deny.Create("Server only accepts client sided positioning!"), socket);
                return;
            }
            else if (data.PositioningType != PositioningTypes.ServerSided &&
                (ServerProperties.ConnectionType == ConnectionTypes.Server || ServerProperties.ConnectionType == ConnectionTypes.Hybrid))
            {
                _ = Signalling.SendPacketAsync(Packets.Signalling.Deny.Create("Server only accepts server sided positioning!"), socket);
                return;
            }
            if (Participants.FirstOrDefault(x => x.Value.SignallingSocket == socket).Value != null)
            {
                _ = Signalling.SendPacketAsync(Packets.Signalling.Deny.Create("Already logged in!"), socket);
                return;
            }

            var key = data.Key;
            var participant = new VoiceCraftParticipant("[N.A.]", socket, data.PositioningType);

            if (Participants.ContainsKey(data.Key))
            {
                for (ushort i = 0; i < ushort.MaxValue; i++)
                {
                    if (!Participants.ContainsKey(i))
                    {
                        key = i;
                        break;
                    }
                }
            }

            Participants.TryAdd(key, participant);
            _ = Signalling.SendPacketAsync(Packets.Signalling.Accept.Create(key, ServerProperties.VoicePortUDP), socket);
        }

        private void Signalling_BindedUnbinded(Packets.Signalling.BindedUnbinded data, Socket socket)
        {
            var participant = Participants.FirstOrDefault(x => x.Value.SignallingSocket == socket);
            if (participant.Value != null && participant.Value.PositioningType == PositioningTypes.ClientSided && !participant.Value.Binded && data.Binded) //data.Binded is the client requesting to bind.
            {
                participant.Value.LastActive = Environment.TickCount;
                participant.Value.Binded = true;
                participant.Value.Name = data.Name;
                var list = Participants.Where(x => x.Key != participant.Key && x.Value.Binded && x.Value.Channel == participant.Value.Channel);
                for (ushort i = 0; i < list.Count(); i++)
                {
                    var client = list.ElementAt(i);
                    _ = Signalling.SendPacketAsync(Packets.Signalling.Login.Create(PositioningTypes.ServerSided, client.Key, client.Value.IsDeafened, client.Value.IsMuted, client.Value.Name, string.Empty), participant.Value.SignallingSocket);

                    _ = Signalling.SendPacketAsync(Packets.Signalling.Login.Create(PositioningTypes.ServerSided, participant.Key, participant.Value.IsDeafened, participant.Value.IsMuted, participant.Value.Name, string.Empty), client.Value.SignallingSocket);
                }

                var channelList = ServerProperties.Channels;
                for (int i = 0; i < channelList.Count; i++)
                {
                    var channel = ServerProperties.Channels[i];
                    _ = Signalling.SendPacketAsync(Packets.Signalling.AddChannel.Create(channel.Name, (byte)(i + 1), !string.IsNullOrWhiteSpace(channel.Password)), participant.Value.SignallingSocket);
                }

                OnParticipantBinded?.Invoke(participant.Value, participant.Key);
            }
            if (participant.Value != null && participant.Value.PositioningType == PositioningTypes.ClientSided && participant.Value.Binded && !data.Binded) //data.Binded is the client requesting to unbind.
            {
                participant.Value.LastActive = Environment.TickCount;
                participant.Value.Binded = false;
                var list = Participants.Where(x => x.Key != participant.Key && x.Value.Binded && x.Value.Channel == participant.Value.Channel);
                for (ushort i = 0; i < list.Count(); i++)
                {
                    var client = list.ElementAt(i);
                    //Logout the unbinded participant from all other clients.
                    _ = Signalling.SendPacketAsync(Packets.Signalling.Logout.Create(participant.Key), client.Value.SignallingSocket);
                }
                OnParticipantUnbinded?.Invoke(participant.Value, participant.Key);
            }
        }

        private void Signalling_MuteUnmute(Packets.Signalling.MuteUnmute data, Socket socket)
        {
            var participant = Participants.FirstOrDefault(x => x.Value.SignallingSocket == socket);
            if (participant.Value != null && participant.Value.IsMuted != data.Value) //data.Value is the participant request.
            {
                participant.Value.LastActive = Environment.TickCount;
                participant.Value.IsMuted = data.Value;
                if (!participant.Value.Binded) return; //Return if not binded because the participants is not on other clients.

                var list = Participants.Where(x => x.Key != participant.Key && x.Value.Binded && x.Value.Channel == participant.Value.Channel);
                for (ushort i = 0; i < list.Count(); i++)
                {
                    var client = list.ElementAt(i);
                    _ = Signalling.SendPacketAsync(Packets.Signalling.MuteUnmute.Create(participant.Key, data.Value), client.Value.SignallingSocket);
                }
            }
        }

        private void Signalling_DeafenUndeafen(Packets.Signalling.DeafenUndeafen data, Socket socket)
        {
            var participant = Participants.FirstOrDefault(x => x.Value.SignallingSocket == socket);
            if (participant.Value != null && participant.Value.IsDeafened != data.Value) //data.Value is the participant request.
            {
                participant.Value.LastActive = Environment.TickCount;
                participant.Value.IsDeafened = data.Value;
                if (!participant.Value.Binded) return; //Return if not binded because the participants is not on other clients.

                var list = Participants.Where(x => x.Key != participant.Key && x.Value.Binded && x.Value.Channel == participant.Value.Channel);
                for (ushort i = 0; i < list.Count(); i++)
                {
                    var client = list.ElementAt(i);
                    _ = Signalling.SendPacketAsync(Packets.Signalling.DeafenUndeafen.Create(participant.Key, data.Value), client.Value.SignallingSocket);
                }
            }
        }

        private void Signalling_JoinLeaveChannel(Packets.Signalling.JoinLeaveChannel data, Socket socket)
        {
            var participant = Participants.FirstOrDefault(x => x.Value.SignallingSocket == socket);
            if (participant.Value != null && participant.Value.Binded)
            {
                participant.Value.LastActive = Environment.TickCount;
                var channel = ServerProperties.Channels.ElementAtOrDefault(data.ChannelId - 1);
                if (channel != null)
                {
                    if (participant.Value.Channel != channel && (channel.Password == data.Password || string.IsNullOrWhiteSpace(channel.Password)) && data.Joined)
                    {
                        _ = MoveParticipantToChannel(channel, participant);
                    }
                    else if (!data.Joined)
                    {
                        _ = MoveParticipantToChannel(null, participant);
                    }
                    else if (channel?.Password != data.Password && !string.IsNullOrWhiteSpace(channel?.Password))
                    {
                        _ = Signalling.SendPacketAsync(Packets.Signalling.Deny.Create("Incorrect Password!"), socket);
                    }
                }
                else
                {
                    _ = Signalling.SendPacketAsync(Packets.Signalling.Deny.Create("Channel does not exist!"), socket);
                }
            }
        }

        private void Signalling_SocketConnected(Socket socket)
        {
            //Wait 5 seconds before checking if the socket is used.
            Task.Delay(5000).ContinueWith(t =>
            {
                var participant = Participants.FirstOrDefault(x => x.Value.SignallingSocket == socket);
                if (participant.Value == null)
                {
                    //If client closed the socket, We catch it otherwise we close the connection.
                    try
                    {
                        socket.Disconnect(false);
                        socket.Close();
                    }
                    catch (ObjectDisposedException)
                    {
                        return;
                    }
                }
            });
        }

        private void Signalling_SocketDisconnected(Socket socket, string? reason = null)
        {
            var participant = Participants.FirstOrDefault(x => x.Value.SignallingSocket == socket);
            _ = RemoveParticipant(participant, true, reason);
        }

        private void Signalling_PingCheck(Packets.Signalling.Null data, Socket socket)
        {
            var participant = Participants.FirstOrDefault(x => x.Value.SignallingSocket == socket);
            if (participant.Value != null)
            {
                participant.Value.LastActive = Environment.TickCount;
                _ = Signalling.SendPacketAsync(Packets.Signalling.Null.Create(SignallingPacketTypes.PingCheck), socket);
            }
        }
        #endregion

        #region Voice
        private void Voice_Login(Packets.Voice.Login data, EndPoint endPoint)
        {
            var participant = Participants.FirstOrDefault(x => x.Value.SignallingSocket.RemoteEndPoint?.ToString()?.Split(':').FirstOrDefault() == endPoint.ToString()?.Split(':').FirstOrDefault() && x.Key == data.Key);
            if (participant.Value != null && participant.Value.VoiceEndpoint == null)
            {
                participant.Value.VoiceEndpoint = endPoint;
                OnParticipantConnected?.Invoke(participant.Value, participant.Key);
                _ = Voice.SendPacketToAsync(Packets.Voice.Null.Create(VoicePacketTypes.Accept), endPoint);
            }
            else
            {
                _ = Voice.SendPacketToAsync(Packets.Voice.Deny.Create("Key is invalid or used!"), endPoint);
            }
        }

        private void Voice_KeepAlive(Packets.Voice.Null data, EndPoint endPoint)
        {
            var participant = Participants.FirstOrDefault(x => x.Value.VoiceEndpoint?.Equals(endPoint) ?? false);
            if (participant.Value != null)
            {
                participant.Value.LastActive = Environment.TickCount;
                _ = Voice.SendPacketToAsync(Packets.Voice.Null.Create(VoicePacketTypes.KeepAlive), endPoint);
            }
        }

        private void Voice_ClientAudio(Packets.Voice.ClientAudio data, EndPoint endPoint)
        {
            _ = Task.Run(async () =>
            {
                var participant = Participants.FirstOrDefault(x => x.Value.VoiceEndpoint?.Equals(endPoint) ?? false);
                if (participant.Value != null &&
                    !participant.Value.IsMuted && !participant.Value.IsDeafened &&
                    !participant.Value.IsServerMuted && participant.Value.Binded)
                {
                    var proximityToggle = participant.Value.Channel?.OverrideSettings?.ProximityToggle ?? ServerProperties.ProximityToggle;
                    if (proximityToggle)
                    {
                        if (participant.Value.IsDead || string.IsNullOrWhiteSpace(participant.Value.EnvironmentId)) return;
                        var proximityDistance = participant.Value.Channel?.OverrideSettings?.ProximityDistance ?? ServerProperties.ProximityDistance;
                        var voiceEffects = participant.Value.Channel?.OverrideSettings?.VoiceEffects ?? ServerProperties.VoiceEffects;

                        var list = Participants.Where(x =>
                        x.Key != participant.Key &&
                        x.Value.Binded &&
                        !x.Value.IsDeafened &&
                        !x.Value.IsDead &&
                        x.Value.Channel == participant.Value.Channel &&
                        !string.IsNullOrWhiteSpace(x.Value.EnvironmentId) &&
                        x.Value.EnvironmentId == participant.Value.EnvironmentId &&
                        Vector3.Distance(x.Value.Position, participant.Value.Position) <= proximityDistance);

                        for (ushort i = 0; i < list.Count(); i++)
                        {
                            var client = list.ElementAt(i);

                            if (client.Value.VoiceEndpoint != null)
                            {
                                var volume = 1.0f - Math.Clamp(Vector3.Distance(client.Value.Position, participant.Value.Position) / proximityDistance, 0.0f, 1.0f);
                                var echo = voiceEffects ? Math.Max(participant.Value.CaveDensity, client.Value.CaveDensity) * (1.0f - volume) : 0.0f;
                                var muffled = voiceEffects && (client.Value.InWater || participant.Value.InWater);
                                var rotation = (float)(Math.Atan2(client.Value.Position.Z - participant.Value.Position.Z, client.Value.Position.X - participant.Value.Position.X) - (client.Value.Rotation * Math.PI / 180));

                                await Voice.SendPacketToAsync(Packets.Voice.ServerAudio.Create(participant.Key, data.PacketCount, volume, echo, rotation, muffled, data.Audio), client.Value.VoiceEndpoint);
                            }
                        }
                    }
                    else
                    {
                        var list = Participants.Where(x =>
                        x.Key != participant.Key &&
                        x.Value.Binded &&
                        !x.Value.IsDeafened &&
                        x.Value.Channel == participant.Value.Channel);

                        for (ushort i = 0; i < list.Count(); i++)
                        {
                            var client = list.ElementAt(i);
                            if (client.Value.VoiceEndpoint != null)
                            {
                                await Voice.SendPacketToAsync(Packets.Voice.ServerAudio.Create(participant.Key, data.PacketCount, 1.0f, 0.0f, 1.5f, false, data.Audio), client.Value.VoiceEndpoint);
                            }
                        }
                    }
                }
            }).ContinueWith(x =>
            {
                if (ServerProperties.Debugger.LogExceptions && x.Exception != null)
                {
                    OnExceptionError?.Invoke(x.Exception);
                }
            });
        }

        private void Voice_UpdatePosition(Packets.Voice.UpdatePosition data, EndPoint endPoint)
        {
            var participant = Participants.FirstOrDefault(x => x.Value.VoiceEndpoint?.ToString() == endPoint.ToString());
            if (participant.Value != null && participant.Value.Binded && participant.Value.PositioningType == PositioningTypes.ClientSided)
            {
                participant.Value.EnvironmentId = data.EnvironmentId;
                participant.Value.Position = data.Position;
            }
        }
        #endregion

        #region MCComm
        private void MCCommLogin(Packets.MCComm.Login packet, HttpListenerContext ctx)
        {
            if (packet.LoginKey != MCComm.ServerKey)
            {
                var denyPacket = Packets.MCComm.Deny.Create("Invalid Key!");
                MCComm.SendResponse(ctx, HttpStatusCode.OK, denyPacket);
                return;
            }
            if (ExternalServers.Exists(x => x.IP == ctx.Request.RemoteEndPoint?.ToString().Split(":").FirstOrDefault()))
            {
                var denyPacket = Packets.MCComm.Deny.Create("Already Logged In!");
                MCComm.SendResponse(ctx, HttpStatusCode.OK, denyPacket);
                return;
            }

            var server = new ExternalServer()
            {
                IP = ctx.Request.RemoteEndPoint?.ToString().Split(":").FirstOrDefault() ?? string.Empty
            };
            ExternalServers.Add(server);
            OnExternalServerConnected?.Invoke(server);

            var acceptPacket = Packets.MCComm.Accept.Create();
            MCComm.SendResponse(ctx, HttpStatusCode.OK, acceptPacket);
        }

        private void MCCommBinded(Packets.MCComm.Bind packet, HttpListenerContext ctx)
        {
            if (!ServerLoggedIn(ctx)) return;

            var participant = Participants.FirstOrDefault(x => x.Key == packet.PlayerKey);
            if (participant.Value == null)
            {
                MCComm.SendResponse(ctx, HttpStatusCode.OK, Packets.MCComm.Deny.Create("Could not find key!"));
                return;
            }
            if (participant.Value.Binded)
            {
                MCComm.SendResponse(ctx, HttpStatusCode.OK, Packets.MCComm.Deny.Create("Key has already been binded to a participant!"));
                return;
            }
            if (Participants.FirstOrDefault(x => x.Value.MinecraftId == packet.PlayerId).Value != null)
            {
                MCComm.SendResponse(ctx, HttpStatusCode.OK, Packets.MCComm.Deny.Create("PlayerId is already binded to a participant!"));
                return;
            }
            if (participant.Value.PositioningType == PositioningTypes.ClientSided)
            {
                MCComm.SendResponse(ctx, HttpStatusCode.OK, Packets.MCComm.Deny.Create("Participant is using client sided positioning!"));
                return;
            }

            participant.Value.Name = packet.Gamertag;
            participant.Value.MinecraftId = packet.PlayerId;
            participant.Value.Binded = true;
            _ = Signalling.SendPacketAsync(Packets.Signalling.BindedUnbinded.Create(participant.Value.Name, true), participant.Value.SignallingSocket);

            MCComm.SendResponse(ctx, HttpStatusCode.OK, Packets.MCComm.Accept.Create());
            var list = Participants.Where(x => x.Key != participant.Key && x.Value.Binded && x.Value.Channel == participant.Value.Channel);
            for (ushort i = 0; i < list.Count(); i++)
            {
                var client = list.ElementAt(i);
                _ = Signalling.SendPacketAsync(Packets.Signalling.Login.Create(PositioningTypes.ServerSided, client.Key, client.Value.IsDeafened, client.Value.IsMuted, client.Value.Name, string.Empty), participant.Value.SignallingSocket);
                _ = Signalling.SendPacketAsync(Packets.Signalling.Login.Create(PositioningTypes.ServerSided, participant.Key, participant.Value.IsDeafened, participant.Value.IsMuted, participant.Value.Name, string.Empty), client.Value.SignallingSocket);
            }

            var channelList = ServerProperties.Channels;
            for (int i = 0; i < channelList.Count; i++)
            {
                var channel = ServerProperties.Channels[i];
                _ = Signalling.SendPacketAsync(Packets.Signalling.AddChannel.Create(channel.Name, (byte)(i + 1), !string.IsNullOrWhiteSpace(channel.Password)), participant.Value.SignallingSocket);
            }

            OnParticipantBinded?.Invoke(participant.Value, participant.Key);
        }

        private void MCCommUpdate(Packets.MCComm.Update packet, HttpListenerContext ctx)
        {
            if (!ServerLoggedIn(ctx)) return;

            for (int i = 0; i < packet.Players.Count; i++)
            {
                var player = packet.Players[i];
                var participant = Participants.FirstOrDefault(x => x.Value.MinecraftId == player.PlayerId && x.Value.PositioningType == PositioningTypes.ServerSided);
                if (participant.Value != null)
                {
                    if (!participant.Value.Position.Equals(player.Location))
                        participant.Value.Position = player.Location;

                    if (participant.Value.EnvironmentId != player.DimensionId)
                        participant.Value.EnvironmentId = player.DimensionId;

                    if (participant.Value.Rotation != player.Rotation)
                        participant.Value.Rotation = player.Rotation;

                    if (participant.Value.CaveDensity != player.CaveDensity)
                        participant.Value.CaveDensity = player.CaveDensity;

                    if (participant.Value.IsDead != player.IsDead)
                        participant.Value.IsDead = player.IsDead;

                    if (participant.Value.InWater != player.InWater)
                        participant.Value.InWater = player.InWater;
                }
            }

            MCComm.SendResponse(ctx, HttpStatusCode.OK, Packets.MCComm.AcceptUpdate.Create(Participants.Values.Where(x => Environment.TickCount - (long)x.LastSpoke < 500).Select(x => x.MinecraftId).ToList()));
        }

        private void MCCommGetSettings(Packets.MCComm.GetSettings packet, HttpListenerContext ctx)
        {
            if (!ServerLoggedIn(ctx)) return;

            var settingsPacket = Packets.MCComm.UpdateSettings.Create(ServerProperties.ProximityDistance, ServerProperties.ProximityToggle, ServerProperties.VoiceEffects);
            MCComm.SendResponse(ctx, HttpStatusCode.OK, settingsPacket);
        }

        private void MCCommUpdateSettings(Packets.MCComm.UpdateSettings packet, HttpListenerContext ctx)
        {
            if (!ServerLoggedIn(ctx)) return;

            if (packet.ProximityDistance < 1 || packet.ProximityDistance > 120)
            {
                MCComm.SendResponse(ctx, HttpStatusCode.OK, Packets.MCComm.Deny.Create("Proximity distance must be between 1 and 120!"));
                return;
            }

            ServerProperties.ProximityDistance = packet.ProximityDistance;
            ServerProperties.ProximityToggle = packet.ProximityToggle;
            ServerProperties.VoiceEffects = packet.VoiceEffects;
            MCComm.SendResponse(ctx, HttpStatusCode.OK, Packets.MCComm.Accept.Create());
        }

        private void MCCommRemoveParticipant(Packets.MCComm.RemoveParticipant packet, HttpListenerContext ctx)
        {
            if (!ServerLoggedIn(ctx)) return;

            var participant = Participants.FirstOrDefault(x => x.Value.MinecraftId == packet.PlayerId);
            if (participant.Value != null)
            {
                _ = RemoveParticipant(participant, true, "MCComm server kicked.");
                OnParticipantDisconnected?.Invoke("MCComm server kicked.", participant.Value, participant.Key);
                return;
            }

            MCComm.SendResponse(ctx, HttpStatusCode.OK, Packets.MCComm.Deny.Create("Could not find participant!"));
        }

        private void MCCommChannelMove(Packets.MCComm.ChannelMove packet, HttpListenerContext ctx)
        {
            if (!ServerLoggedIn(ctx)) return;
            var participant = Participants.FirstOrDefault(x => x.Value.MinecraftId == packet.PlayerId);
            var channel = ServerProperties.Channels.ElementAtOrDefault(packet.ChannelId - 1);

            if (participant.Value == null)
            {
                MCComm.SendResponse(ctx, HttpStatusCode.OK, Packets.MCComm.Deny.Create("Could not find participant!"));
                return;
            }

            if (channel == null && packet.ChannelId > 0)
            {
                MCComm.SendResponse(ctx, HttpStatusCode.OK, Packets.MCComm.Deny.Create("Channel does not exist!"));
                return;
            }

            if (channel == participant.Value.Channel)
            {
                MCComm.SendResponse(ctx, HttpStatusCode.OK, Packets.MCComm.Deny.Create("Participant is already in the channel!"));
                return;
            }

            if (packet.ChannelId == 0 && participant.Value.Channel != null)
            {
                _ = MoveParticipantToChannel(null, participant);
                MCComm.SendResponse(ctx, HttpStatusCode.OK, Packets.MCComm.Accept.Create());
                return;
            }

            if (channel != null)
            {
                _ = MoveParticipantToChannel(channel, participant);
                MCComm.SendResponse(ctx, HttpStatusCode.OK, Packets.MCComm.Accept.Create());
                return;
            }
        }

        private bool ServerLoggedIn(HttpListenerContext ctx)
        {
            var server = ExternalServers.FirstOrDefault(x => x.IP == ctx.Request.RemoteEndPoint?.ToString().Split(":").FirstOrDefault());
            if (server == null)
            {
                var denyPacket = Packets.MCComm.Deny.Create("Not Logged In!");
                MCComm.SendResponse(ctx, HttpStatusCode.OK, denyPacket);
                return false;
            }
            else
            {
                server.LastActive = Environment.TickCount;
                return true;
            }
        }
        #endregion

        #region Public Methods
        public void Stop()
        {
            if (ServerState == ServerState.Stopped) return; //Already stopped

            foreach(var participant in Participants)
            {
                participant.Value.SignallingSocket.DisconnectAsync(false);
            }
            CTS.Cancel();
            Signalling.StopHosting();
            Voice.StopHosting();
            if (ServerProperties.ConnectionType == ConnectionTypes.Server || ServerProperties.ConnectionType == ConnectionTypes.Hybrid)
            {
                MCComm.Stop();
            }
            ActivityChecker?.Wait();
            ActivityChecker?.Dispose();
            ServerState = ServerState.Stopped;
        }

        public async Task<bool> RemoveParticipant(ushort key, bool broadcast = true, string? reason = null)
        {
            var participant = Participants.FirstOrDefault(x => x.Key == key);
            if (participant.Value != null)
            {
                return await RemoveParticipant(participant, broadcast, reason);
            }
            return false;
        }

        public async Task<bool> RemoveParticipant(KeyValuePair<ushort, VoiceCraftParticipant> participant, bool broadcast = true, string? reason = null)
        {
            if (participant.Value != null)
            {
                Participants.TryRemove(participant.Key, out _);
                OnParticipantDisconnected?.Invoke(reason ?? "No Reason", participant.Value, participant.Key);

                if (broadcast)
                {
                    await Signalling.SendPacketAsync(Packets.Signalling.Logout.Create(participant.Key), participant.Value.SignallingSocket);
                    foreach (var client in Participants)
                    {
                        if (client.Value.Channel == participant.Value.Channel && client.Value.Binded)
                            await Signalling.SendPacketAsync(Packets.Signalling.Logout.Create(participant.Key), client.Value.SignallingSocket);
                    }
                }
                return true;
            }
            return false;
        }

        public async Task MoveParticipantToChannel(VoiceCraftChannel? toChannel, KeyValuePair<ushort, VoiceCraftParticipant> participant)
        {
            if (participant.Value.Channel == toChannel) return;

            if(participant.Value.Channel != null) 
                await Signalling.SendPacketAsync(Packets.Signalling.JoinLeaveChannel.Create((byte)(ServerProperties.Channels.IndexOf(participant.Value.Channel) + 1), string.Empty, false), participant.Value.SignallingSocket);

            var channelList = Participants.Where(x => x.Key != participant.Key && x.Value.Binded && x.Value.Channel == participant.Value.Channel);
            for (ushort i = 0; i < channelList.Count(); i++)
            {
                var client = channelList.ElementAt(i);
                await Signalling.SendPacketAsync(Packets.Signalling.Logout.Create(participant.Key), client.Value.SignallingSocket);
                await Signalling.SendPacketAsync(Packets.Signalling.Logout.Create(client.Key), participant.Value.SignallingSocket);
            }
            participant.Value.Channel = toChannel;

            if (participant.Value.Channel != null)
                await Signalling.SendPacketAsync(Packets.Signalling.JoinLeaveChannel.Create((byte)(ServerProperties.Channels.IndexOf(participant.Value.Channel) + 1), string.Empty, true), participant.Value.SignallingSocket);

            channelList = Participants.Where(x => x.Key != participant.Key && x.Value.Binded && x.Value.Channel == participant.Value.Channel);
            for (ushort i = 0; i < channelList.Count(); i++)
            {
                var client = channelList.ElementAt(i);
                await Signalling.SendPacketAsync(Packets.Signalling.Login.Create(PositioningTypes.ServerSided, participant.Key, participant.Value.IsDeafened, participant.Value.IsMuted, participant.Value.Name, string.Empty), client.Value.SignallingSocket);
                await Signalling.SendPacketAsync(Packets.Signalling.Login.Create(PositioningTypes.ServerSided, client.Key, client.Value.IsDeafened, client.Value.IsMuted, client.Value.Name, string.Empty), participant.Value.SignallingSocket);
            }
        }
        #endregion

        private async Task ServerChecks()
        {
            while(ServerState != ServerState.Stopped && !CTS.IsCancellationRequested)
            {
                for (int i = ExternalServers.Count - 1; i >= 0; i--)
                {
                    var server = ExternalServers[i];

                    if (Environment.TickCount - (long)server.LastActive > ServerProperties.ExternalServerTimeoutMS)
                    {
                        ExternalServers.RemoveAt(i);
                        OnExternalServerDisconnected?.Invoke(server, $"Timeout - Last Active: {Environment.TickCount - (long)server.LastActive}ms");
                    }
                }
                for (int i = Participants.Count - 1; i >= 0; i--)
                {
                    var participant = Participants.ElementAt(i);

                    if (Environment.TickCount - (long)participant.Value.LastActive > ServerProperties.ClientTimeoutMS)
                    {
                        _ = RemoveParticipant(participant, true, $"Timeout - Last Active: {Environment.TickCount - (long)participant.Value.LastActive}ms");
                    }
                }

                await Task.Delay(ActivityInterval).ConfigureAwait(false);
            }
        }

        ~VoiceCraftServer()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    Signalling.Dispose();
                    Voice.Dispose();
                    MCComm.Dispose();
                    CTS.Cancel();
                    ServerState = ServerState.Stopped;
                    if (ActivityChecker != null)
                    {
                        ActivityChecker.Wait(); //Wait to finish before disposing.
                        ActivityChecker.Dispose();
                        ActivityChecker = null;
                    }
                    IsDisposed = true;
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }

    public enum ServerState
    {
        Starting,
        Started,
        Stopped
    }
}