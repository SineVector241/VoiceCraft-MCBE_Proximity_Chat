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
        public ConcurrentDictionary<int, VoiceCraftParticipant> Participants { get; set; } = new ConcurrentDictionary<int, VoiceCraftParticipant>();
        public List<ExternalServer> ExternalServers { get; set; } = new List<ExternalServer>();
        public ServerState ServerState { get; set; } = ServerState.Stopped;

        public Properties ServerProperties { get; set; } = new Properties();
        public Banlist Banlist { get; set; } = new Banlist();
        private CancellationTokenSource CTS { get; } = new CancellationTokenSource();
        private Task? ActivityChecker { get; set; }
        private Random Randomizer { get; set; } = new Random();
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
        public delegate void ParticipantConnected(VoiceCraftParticipant participant, int privateId);
        public delegate void ParticipantBinded(VoiceCraftParticipant participant, int privateId);
        public delegate void ParticipantUnbinded(VoiceCraftParticipant participant, int privateId);
        public delegate void ParticipantDisconnected(string reason, VoiceCraftParticipant participant, int privateId);
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
            if(Participants.Count >= ushort.MaxValue)
            {
                _ = Signalling.SendPacketAsync(Packets.Signalling.Deny.Create("Server Full!"), socket);
                return;
            }

            var publicId = data.PublicId;
            //We don't want to make the absolute minimum value a valid ID since it will be used to say no ID was set.
            var privateId = Randomizer.Next(int.MinValue + 1, int.MaxValue);
            if (Participants.ContainsKey(privateId))
            {
                for (int i = int.MinValue + 1; i < int.MaxValue; i++)
                {
                    if (!Participants.ContainsKey(i))
                    {
                        privateId = i;
                        break;
                    }
                }
            }

            if(Participants.Values.FirstOrDefault(x => x.PublicId == publicId) != null)
            {
                for (ushort i = ushort.MinValue; i < ushort.MaxValue; i++)
                {
                    if (Participants.Values.FirstOrDefault(x => x.PublicId == publicId) == null)
                    {
                        publicId = i;
                        break;
                    }
                }
            }

            var participant = new VoiceCraftParticipant("[N.A.]", publicId, socket, data.PositioningType);
            Participants.TryAdd(privateId, participant);
            _ = Signalling.SendPacketAsync(Packets.Signalling.Accept.Create(privateId, publicId, ServerProperties.VoicePortUDP), socket);
        }

        private void Signalling_BindedUnbinded(Packets.Signalling.BindedUnbinded data, Socket socket)
        {
            var found = Participants.TryGetValue(data.PrivateId, out var participant) && participant.SignallingSocket == socket;
            if (found && participant?.PositioningType == PositioningTypes.ClientSided && !participant.Binded && data.Binded) //data.Binded is the client requesting to bind.
            {
                participant.LastActive = Environment.TickCount;
                participant.Binded = true;
                participant.Name = data.Name;
                var list = Participants.Where(x => x.Key != data.PrivateId && x.Value.Binded && x.Value.Channel == participant.Channel);
                for (ushort i = 0; i < list.Count(); i++)
                {
                    var client = list.ElementAt(i);
                    _ = Signalling.SendPacketAsync(Packets.Signalling.Login.Create(PositioningTypes.ServerSided, client.Value.PublicId, client.Value.IsDeafened, client.Value.IsMuted, client.Value.Name, string.Empty), participant.SignallingSocket);

                    _ = Signalling.SendPacketAsync(Packets.Signalling.Login.Create(PositioningTypes.ServerSided, participant.PublicId, participant.IsDeafened, participant.IsMuted, participant.Name, string.Empty), client.Value.SignallingSocket);
                }

                var channelList = ServerProperties.Channels;
                for (int i = 0; i < channelList.Count; i++)
                {
                    var channel = ServerProperties.Channels[i];
                    _ = Signalling.SendPacketAsync(Packets.Signalling.AddChannel.Create(channel.Name, (byte)(i + 1), !string.IsNullOrWhiteSpace(channel.Password)), participant.SignallingSocket);
                }

                OnParticipantBinded?.Invoke(participant, data.PrivateId);
            }
            if (found && participant?.PositioningType == PositioningTypes.ClientSided && participant.Binded && !data.Binded) //data.Binded is the client requesting to unbind.
            {
                participant.LastActive = Environment.TickCount;
                participant.Binded = false;
                var list = Participants.Where(x => x.Key != data.PrivateId && x.Value.Binded && x.Value.Channel == participant.Channel);
                for (ushort i = 0; i < list.Count(); i++)
                {
                    var client = list.ElementAt(i);
                    //Logout the unbinded participant from all other clients. int.MinValue is basically no PrivateId.
                    _ = Signalling.SendPacketAsync(Packets.Signalling.Logout.Create(int.MinValue, participant.PublicId), client.Value.SignallingSocket);
                }
                OnParticipantUnbinded?.Invoke(participant, data.PrivateId);
            }
        }

        private void Signalling_MuteUnmute(Packets.Signalling.MuteUnmute data, Socket socket)
        {
            if (Participants.TryGetValue(data.PrivateId, out var participant) && participant.SignallingSocket == socket && participant.IsMuted != data.Value) //data.Value is the participant request.
            {
                participant.LastActive = Environment.TickCount;
                participant.IsMuted = data.Value;
                if (!participant.Binded) return; //Return if not binded because the participants is not on other clients.

                var list = Participants.Where(x => x.Key != data.PrivateId && x.Value.Binded && x.Value.Channel == participant.Channel);
                for (ushort i = 0; i < list.Count(); i++)
                {
                    var client = list.ElementAt(i);
                    //Tell all other clients that the client muted.
                    _ = Signalling.SendPacketAsync(Packets.Signalling.MuteUnmute.Create(int.MinValue, participant.PublicId, data.Value), client.Value.SignallingSocket); //Private Id is set to int.MinValue so we don't leak the Id.
                }
            }
        }

        private void Signalling_DeafenUndeafen(Packets.Signalling.DeafenUndeafen data, Socket socket)
        {
            if (Participants.TryGetValue(data.PrivateId, out var participant) && participant.SignallingSocket == socket && participant.IsDeafened != data.Value) //data.Value is the participant request.
            {
                participant.LastActive = Environment.TickCount;
                participant.IsDeafened = data.Value;
                if (!participant.Binded) return; //Return if not binded because the participants is not on other clients.

                var list = Participants.Where(x => x.Key != data.PrivateId && x.Value.Binded && x.Value.Channel == participant.Channel);
                for (ushort i = 0; i < list.Count(); i++)
                {
                    var client = list.ElementAt(i);
                    _ = Signalling.SendPacketAsync(Packets.Signalling.DeafenUndeafen.Create(int.MinValue, participant.PublicId, data.Value), client.Value.SignallingSocket); //Private Id is set to int.MinValue so we don't leak the Id.
                }
            }
        }

        private void Signalling_JoinLeaveChannel(Packets.Signalling.JoinLeaveChannel data, Socket socket)
        {
            if (Participants.TryGetValue(data.PrivateId, out var participant) && participant.SignallingSocket == socket && participant.Binded)
            {
                participant.LastActive = Environment.TickCount;
                var channel = ServerProperties.Channels.ElementAtOrDefault(data.ChannelId - 1);
                if (channel != null)
                {
                    if (participant.Channel != channel && (channel.Password == data.Password || string.IsNullOrWhiteSpace(channel.Password)) && data.Joined)
                    {
                        _ = MoveParticipantToChannel(channel, participant, int.MinValue); //Don't need to send the private Id back.
                    }
                    else if (!data.Joined)
                    {
                        _ = MoveParticipantToChannel(null, participant, int.MinValue); //Don't need to send the private Id back.
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
                    //If client closed the socket, We catch it, otherwise we close the connection.
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

        private void Signalling_PingCheck(Packets.Signalling.PingCheck data, Socket socket)
        {
            if (Participants.TryGetValue(data.PrivateId, out var participant) && participant.SignallingSocket == socket)
            {
                participant.LastActive = Environment.TickCount;
                _ = Signalling.SendPacketAsync(Packets.Signalling.PingCheck.Create(int.MinValue), socket);
            }
        }
        #endregion

        #region Voice
        private void Voice_Login(Packets.Voice.Login data, EndPoint endPoint)
        {
            if (Participants.TryGetValue(data.PrivateId, out var participant) && participant.SignallingSocket.RemoteEndPoint != null && ((IPEndPoint)participant.SignallingSocket.RemoteEndPoint).Address == ((IPEndPoint)endPoint).Address && participant.VoiceEndpoint == null)
            {
                participant.VoiceEndpoint = endPoint;
                OnParticipantConnected?.Invoke(participant, data.PrivateId);
                _ = Voice.SendPacketToAsync(Packets.Voice.Null.Create(VoicePacketTypes.Accept), endPoint);
            }
            else
            {
                _ = Voice.SendPacketToAsync(Packets.Voice.Deny.Create("Key is invalid or used!"), endPoint);
            }
        }

        private void Voice_KeepAlive(Packets.Voice.KeepAlive data, EndPoint endPoint)
        {
            //Get participant by private ID and see if the source IP address is the same.
            if (Participants.TryGetValue(data.PrivateId, out var participant) && participant.VoiceEndpoint != null && ((IPEndPoint)participant.VoiceEndpoint).Address == ((IPEndPoint)endPoint).Address)
            {
                participant.LastActive = Environment.TickCount;
                //Update endpoint if it has changed, BECAUSE NAT IS FUCKING ANNOYING!
                if (!participant.VoiceEndpoint.Equals(endPoint))
                    participant.VoiceEndpoint = endPoint;

                _ = Voice.SendPacketToAsync(Packets.Voice.KeepAlive.Create(int.MinValue), endPoint);
            }
        }

        private void Voice_ClientAudio(Packets.Voice.ClientAudio data, EndPoint endPoint)
        {
            _ = Task.Run(async () =>
            {
                var found = Participants.TryGetValue(data.PrivateId, out var participant) && participant.VoiceEndpoint != null && ((IPEndPoint)participant.VoiceEndpoint).Address == ((IPEndPoint)endPoint).Address;
                if (participant != null &&
                    !participant.IsMuted && !participant.IsDeafened &&
                    !participant.IsServerMuted && participant.Binded)
                {
                    var proximityToggle = participant.Channel?.OverrideSettings?.ProximityToggle ?? ServerProperties.ProximityToggle;
                    if (proximityToggle)
                    {
                        if (participant.IsDead || string.IsNullOrWhiteSpace(participant.EnvironmentId)) return;
                        var proximityDistance = participant.Channel?.OverrideSettings?.ProximityDistance ?? ServerProperties.ProximityDistance;
                        var voiceEffects = participant.Channel?.OverrideSettings?.VoiceEffects ?? ServerProperties.VoiceEffects;

                        var list = Participants.Where(x =>
                        x.Key != data.PrivateId &&
                        x.Value.Binded &&
                        !x.Value.IsDeafened &&
                        !x.Value.IsDead &&
                        x.Value.Channel == participant.Channel &&
                        !string.IsNullOrWhiteSpace(x.Value.EnvironmentId) &&
                        x.Value.EnvironmentId == participant.EnvironmentId &&
                        Vector3.Distance(x.Value.Position, participant.Position) <= proximityDistance);

                        for (ushort i = 0; i < list.Count(); i++)
                        {
                            var client = list.ElementAt(i);

                            if (client.Value.VoiceEndpoint != null)
                            {
                                var volume = 1.0f - Math.Clamp(Vector3.Distance(client.Value.Position, participant.Position) / proximityDistance, 0.0f, 1.0f);
                                var echo = voiceEffects ? Math.Max(participant.CaveDensity, client.Value.CaveDensity) * (1.0f - volume) : 0.0f;
                                var muffled = voiceEffects && (client.Value.InWater || participant.InWater);
                                var rotation = (float)(Math.Atan2(client.Value.Position.Z - participant.Position.Z, client.Value.Position.X - participant.Position.X) - (client.Value.Rotation * Math.PI / 180));

                                await Voice.SendPacketToAsync(Packets.Voice.ServerAudio.Create(participant.PublicId, data.PacketCount, volume, echo, rotation, muffled, data.Audio), client.Value.VoiceEndpoint);
                            }
                        }
                    }
                    else if (found)
                    {
                        var list = Participants.Where(x =>
                        x.Key != data.PrivateId &&
                        x.Value.Binded &&
                        !x.Value.IsDeafened &&
                        x.Value.Channel == participant.Channel);

                        for (ushort i = 0; i < list.Count(); i++)
                        {
                            var client = list.ElementAt(i);
                            if (client.Value.VoiceEndpoint != null)
                            {
                                await Voice.SendPacketToAsync(Packets.Voice.ServerAudio.Create(participant.PublicId, data.PacketCount, 1.0f, 0.0f, 1.5f, false, data.Audio), client.Value.VoiceEndpoint);
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
                MCComm.SendResponse(ctx, HttpStatusCode.OK, Packets.MCComm.Accept.Create());
                return;
            }

            var server = new ExternalServer()
            {
                IP = ctx.Request.RemoteEndPoint?.ToString().Split(":").FirstOrDefault() ?? string.Empty
            };
            ExternalServers.Add(server);
            OnExternalServerConnected?.Invoke(server);

            MCComm.SendResponse(ctx, HttpStatusCode.OK, Packets.MCComm.Accept.Create());
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
            _ = Signalling.SendPacketAsync(Packets.Signalling.BindedUnbinded.Create(int.MinValue, participant.Value.Name, true), participant.Value.SignallingSocket);

            MCComm.SendResponse(ctx, HttpStatusCode.OK, Packets.MCComm.Accept.Create());
            var list = Participants.Where(x => x.Key != participant.Key && x.Value.Binded && x.Value.Channel == participant.Value.Channel);
            for (ushort i = 0; i < list.Count(); i++)
            {
                var client = list.ElementAt(i);
                _ = Signalling.SendPacketAsync(Packets.Signalling.Login.Create(PositioningTypes.ServerSided, client.Value.PublicId, client.Value.IsDeafened, client.Value.IsMuted, client.Value.Name, string.Empty), participant.Value.SignallingSocket);
                _ = Signalling.SendPacketAsync(Packets.Signalling.Login.Create(PositioningTypes.ServerSided, participant.Value.PublicId, participant.Value.IsDeafened, participant.Value.IsMuted, participant.Value.Name, string.Empty), client.Value.SignallingSocket);
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
                _ = MoveParticipantToChannel(null, participant.Value, participant.Key);
                MCComm.SendResponse(ctx, HttpStatusCode.OK, Packets.MCComm.Accept.Create());
                return;
            }

            if (channel != null)
            {
                _ = MoveParticipantToChannel(channel, participant.Value, participant.Key);
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

        public async Task<bool> RemoveParticipant(KeyValuePair<int, VoiceCraftParticipant> participant, bool broadcast = true, string? reason = null)
        {
            if (participant.Value != null)
            {
                Participants.TryRemove(participant.Key, out _);
                OnParticipantDisconnected?.Invoke(reason ?? "No Reason", participant.Value, participant.Key);

                if (broadcast)
                {
                    //Tell the client to logout, We use the private Id to tell the client to logout.
                    await Signalling.SendPacketAsync(Packets.Signalling.Logout.Create(participant.Key, 0), participant.Value.SignallingSocket);
                    foreach (var client in Participants)
                    {
                        if (client.Value.Channel == participant.Value.Channel && client.Value.Binded)
                            await Signalling.SendPacketAsync(Packets.Signalling.Logout.Create(int.MinValue, participant.Value.PublicId), client.Value.SignallingSocket); //Tell all other clients that the client logged out.
                    }
                }
                return true;
            }
            return false;
        }

        public async Task MoveParticipantToChannel(VoiceCraftChannel? toChannel, VoiceCraftParticipant participant, int privateId)
        {
            if (participant.Channel == toChannel) return;

            if(participant.Channel != null) 
                await Signalling.SendPacketAsync(Packets.Signalling.JoinLeaveChannel.Create(int.MinValue, (byte)(ServerProperties.Channels.IndexOf(participant.Channel) + 1), string.Empty, false), participant.SignallingSocket);

            var channelList = Participants.Where(x => x.Key != privateId && x.Value.Binded && x.Value.Channel == participant.Channel);
            for (ushort i = 0; i < channelList.Count(); i++)
            {
                var client = channelList.ElementAt(i);
                await Signalling.SendPacketAsync(Packets.Signalling.Logout.Create(int.MinValue, participant.PublicId), client.Value.SignallingSocket);
                await Signalling.SendPacketAsync(Packets.Signalling.Logout.Create(int.MinValue, client.Value.PublicId), participant.SignallingSocket);
            }
            participant.Channel = toChannel;

            if (participant.Channel != null)
                await Signalling.SendPacketAsync(Packets.Signalling.JoinLeaveChannel.Create(int.MinValue, (byte)(ServerProperties.Channels.IndexOf(participant.Channel) + 1), string.Empty, true), participant.SignallingSocket);

            channelList = Participants.Where(x => x.Key != privateId && x.Value.Binded && x.Value.Channel == participant.Channel);
            for (ushort i = 0; i < channelList.Count(); i++)
            {
                var client = channelList.ElementAt(i);
                await Signalling.SendPacketAsync(Packets.Signalling.Login.Create(PositioningTypes.ServerSided, participant.PublicId, participant.IsDeafened, participant.IsMuted, participant.Name, string.Empty), client.Value.SignallingSocket);
                await Signalling.SendPacketAsync(Packets.Signalling.Login.Create(PositioningTypes.ServerSided, client.Value.PublicId, client.Value.IsDeafened, client.Value.IsMuted, client.Value.Name, string.Empty), participant.SignallingSocket);
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