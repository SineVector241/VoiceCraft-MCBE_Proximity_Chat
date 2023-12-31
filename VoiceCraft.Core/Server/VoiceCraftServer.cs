using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using VoiceCraft.Core.Packets;
using VoiceCraft.Core.Server.Sockets;

namespace VoiceCraft.Core.Server
{
    public class VoiceCraftServer
    {
        //Constants
        public const string Version = "v1.0.1";
        public const int ActivityInterval = 5000;

        //Data
        public ConcurrentDictionary<ushort, VoiceCraftParticipant> Participants = new ConcurrentDictionary<ushort, VoiceCraftParticipant>();
        public List<ExternalServer> ExternalServers = new List<ExternalServer>();

        public Properties ServerProperties { get; set; } = new Properties();
        public Banlist Banlist { get; set; } = new Banlist();
        private CancellationTokenSource CTS { get; } = new CancellationTokenSource();
        private System.Timers.Timer ActivityChecker { get; set; }

        //Sockets
        public SignallingSocket Signalling { get; set; }
        public VoiceSocket Voice { get; set; }
        public MCCommSocket MCComm { get; set; }

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
        public event Error? OnError;

        public VoiceCraftServer()
        {
            Signalling = new SignallingSocket(CTS.Token);
            Voice = new VoiceSocket(CTS.Token);
            MCComm = new MCCommSocket(CTS.Token);

            Signalling.OnStarted += SignallingSocketStarted;
            Voice.OnStarted += VoiceSocketStarted;
            MCComm.OnStarted += MCCommStarted;

            ActivityChecker = new System.Timers.Timer(ActivityInterval);
            ActivityChecker.Elapsed += DoServerChecks;

            //Event methods in order!
            //Signalling
            Signalling.OnLoginPacketReceived += SignallingLogin;
            Signalling.OnBindedPacketReceived += SignallingBinded;
            Signalling.OnMutePacketReceived += SignallingMute;
            Signalling.OnUnmutePacketReceived += SignallingUnmute;
            Signalling.OnDeafenPacketReceived += SignallingDeafen;
            Signalling.OnUndeafenPacketReceived += SignallingUndeafen;
            Signalling.OnUnbindedPacketReceived += SignallingUnbinded;
            Signalling.OnPingCheckPacketReceived += SignallingPingCheck;
            Signalling.OnJoinChannelPacketReceived += SignallingJoinChannel;
            Signalling.OnLeaveChannelPacketReceived += SignallingLeaveChannel;

            //Voice
            Voice.OnLoginPacketReceived += OnVoiceLogin;
            Voice.OnClientAudioPacketReceived += VoiceClientAudio;
            Voice.OnUpdatePositionPacketReceived += VoiceUpdatePosition;

            //MCComm
            MCComm.OnLoginPacketReceived += MCCommLogin;
            MCComm.OnBindedPacketReceived += MCCommBinded;
            MCComm.OnUpdatePacketReceived += MCCommUpdate;
            MCComm.OnGetSettingsPacketReceived += MCCommGetSettings;
            MCComm.OnUpdateSettingsPacketReceived += MCCommUpdateSettings;
            MCComm.OnRemoveParticipantPacketReceived += MCCommRemoveParticipant;

            //Ping Packet
            Signalling.OnPingPacketReceived += PingReceived;
            Signalling.OnSocketConnected += SignallingSocketConnected;
            Signalling.OnSocketDisconnected += SignallingSocketDisconnected;
        }

        public void Start()
        {
            ActivityChecker.Start();

            _ = Task.Run(() =>
            {
                try
                {
                    Signalling.LogInbound = ServerProperties.Debugger.LogInboundSignallingPackets;
                    Signalling.LogOutbound = ServerProperties.Debugger.LogOutboundSignallingPackets;
                    Signalling.InboundFilter = ServerProperties.Debugger.InboundSignallingFilter;
                    Signalling.OutboundFilter = ServerProperties.Debugger.OutboundSignallingFilter;
                    Signalling.LogExceptions = ServerProperties.Debugger.LogExceptions;
                    Signalling.Start(ServerProperties.SignallingPortTCP);
                }
                catch (Exception ex)
                {
                    OnError?.Invoke(ex);
                    CTS.Cancel();
                }
            });
        }

        private void DoServerChecks(object sender, System.Timers.ElapsedEventArgs e)
        {
            for(int i = ExternalServers.Count - 1; i >= 0; i--)
            {
                var server = ExternalServers[i];

                if (DateTime.UtcNow.Subtract(server.LastActive).TotalMilliseconds > ServerProperties.ExternalServerTimeoutMS)
                {
                    ExternalServers.RemoveAt(i);
                    OnExternalServerDisconnected?.Invoke(server, "Timeout");
                }
            }

            for (int i = Participants.Count - 1; i >= 0; i--)
            {
                var participant = Participants.ElementAt(i);

                if (DateTime.UtcNow.Subtract(participant.Value.LastActive).TotalMilliseconds > ServerProperties.ClientTimeoutMS)
                {
                    RemoveParticipant(participant, true, "Timeout");
                }
            }
        }

        //Event Methods
        #region Signalling
        private void PingReceived(Packets.Signalling.Ping packet, Socket socket)
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

        private void SignallingSocketStarted()
        {
            OnSignallingStarted?.Invoke();
            _ = Task.Run(() =>
            {
                try
                {
                    Voice.LogInbound = ServerProperties.Debugger.LogInboundVoicePackets;
                    Voice.LogOutbound = ServerProperties.Debugger.LogOutboundVoicePackets;
                    Voice.InboundFilter = ServerProperties.Debugger.InboundVoiceFilter;
                    Voice.OutboundFilter = ServerProperties.Debugger.OutboundVoiceFilter;
                    Voice.LogExceptions = ServerProperties.Debugger.LogExceptions;
                    Voice.Start(ServerProperties.VoicePortUDP);
                }
                catch (Exception ex)
                {
                    OnError?.Invoke(ex);
                    CTS.Cancel();
                }
            });
        }

        private void SignallingLogin(Packets.Signalling.Login packet, Socket socket)
        {
            if (Version != packet.Version)
            {
                Signalling.SendPacketAsync(Packets.Signalling.Deny.Create("Versions do not match!", true), socket);
                return;
            }
            if (Banlist.IPBans.Exists(x => x == socket.RemoteEndPoint.ToString()?.Split(':').FirstOrDefault()))
            {
                Signalling.SendPacketAsync(Packets.Signalling.Deny.Create("You have been banned from the server!", true), socket);
                return;
            }
            if (packet.PositioningType != PositioningTypes.ClientSided &&
                (ServerProperties.ConnectionType == ConnectionTypes.Client || ServerProperties.ConnectionType == ConnectionTypes.Hybrid))
            {
                Signalling.SendPacketAsync(Packets.Signalling.Deny.Create("Server only accepts client sided positioning!", true), socket);
                return;
            }
            else if (packet.PositioningType != PositioningTypes.ServerSided &&
                (ServerProperties.ConnectionType == ConnectionTypes.Server || ServerProperties.ConnectionType == ConnectionTypes.Hybrid))
            {
                Signalling.SendPacketAsync(Packets.Signalling.Deny.Create("Server only accepts server sided positioning!", true), socket);
                return;
            }
            if (Participants.FirstOrDefault(x => x.Value.SignallingSocket == socket).Value != null)
            {
                Signalling.SendPacketAsync(Packets.Signalling.Deny.Create("Already logged in!", true), socket);
                return;
            }

            var key = packet.LoginKey;
            Signalling.ConnectedSockets.TryGetValue(socket, out var stream);
            var participant = new VoiceCraftParticipant(socket, packet.PositioningType);

            if (Participants.ContainsKey(packet.LoginKey))
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
            Signalling.SendPacketAsync(Packets.Signalling.Accept.Create(key, ServerProperties.VoicePortUDP), socket);
        }

        private void SignallingBinded(Packets.Signalling.Binded packet, Socket socket)
        {
            var participant = Participants.FirstOrDefault(x => x.Value.SignallingSocket == socket);
            if (participant.Value != null && participant.Value.PositioningType == PositioningTypes.ClientSided && !participant.Value.Binded)
            {
                participant.Value.LastActive = DateTime.UtcNow;
                participant.Value.Binded = true;
                participant.Value.Name = packet.Name;
                var list = Participants.Where(x => x.Key != participant.Key && x.Value.Binded && x.Value.Channel == participant.Value.Channel);
                for (ushort i = 0; i < list.Count(); i++)
                {
                    var client = list.ElementAt(i);
                    Signalling.SendPacketAsync(Packets.Signalling.Login.Create(PositioningTypes.ServerSided, client.Key, client.Value.IsDeafened, client.Value.IsMuted, client.Value.Name, string.Empty), participant.Value.SignallingSocket);

                    Signalling.SendPacketAsync(Packets.Signalling.Login.Create(PositioningTypes.ServerSided, participant.Key, participant.Value.IsDeafened, participant.Value.IsMuted, participant.Value.Name, string.Empty), client.Value.SignallingSocket);
                }

                var channelList = ServerProperties.Channels;
                for (int i = 0; i < channelList.Count; i++)
                {
                    var channel = ServerProperties.Channels[i];
                    Signalling.SendPacketAsync(Packets.Signalling.AddChannel.Create(channel.Name, (byte)(i + 1), !string.IsNullOrWhiteSpace(channel.Password)), participant.Value.SignallingSocket);
                }

                OnParticipantBinded?.Invoke(participant.Value, participant.Key);
            }
        }

        private void SignallingUnbinded(Packets.Signalling.Unbinded packet, Socket socket)
        {
            var participant = Participants.FirstOrDefault(x => x.Value.SignallingSocket == socket);
            if (participant.Value != null && participant.Value.PositioningType == PositioningTypes.ClientSided && participant.Value.Binded)
            {
                participant.Value.LastActive = DateTime.UtcNow;
                participant.Value.Binded = false;
                var list = Participants.Where(x => x.Key != participant.Key && x.Value.Binded && x.Value.Channel == participant.Value.Channel);
                for (ushort i = 0; i < list.Count(); i++)
                {
                    var client = list.ElementAt(i);
                    //Logout the unbinded participant from all other clients.
                    Signalling.SendPacketAsync(Packets.Signalling.Logout.Create(participant.Key), client.Value.SignallingSocket);
                }
                OnParticipantUnbinded?.Invoke(participant.Value, participant.Key);
            }
        }

        private void SignallingMute(Packets.Signalling.Mute packet, Socket socket)
        {
            var participant = Participants.FirstOrDefault(x => x.Value.SignallingSocket == socket);
            if (participant.Value != null && !participant.Value.IsMuted)
            {
                participant.Value.LastActive = DateTime.UtcNow;
                participant.Value.IsMuted = true;
                if (!participant.Value.Binded) return; //Return if not binded because the participants is not on other clients.

                var list = Participants.Where(x => x.Key != participant.Key && x.Value.Binded && x.Value.Channel == participant.Value.Channel);
                for (ushort i = 0; i < list.Count(); i++)
                {
                    var client = list.ElementAt(i);
                    Signalling.SendPacketAsync(Packets.Signalling.Mute.Create(participant.Key), client.Value.SignallingSocket);
                }
            }
        }

        private void SignallingUnmute(Packets.Signalling.Unmute packet, Socket socket)
        {
            var participant = Participants.FirstOrDefault(x => x.Value.SignallingSocket == socket);
            if (participant.Value != null && participant.Value.IsMuted)
            {
                participant.Value.LastActive = DateTime.UtcNow;
                participant.Value.IsMuted = false;
                if (!participant.Value.Binded) return; //Return if not binded because the participants is not on other clients.

                var list = Participants.Where(x => x.Key != participant.Key && x.Value.Binded && x.Value.Channel == participant.Value.Channel);
                for (ushort i = 0; i < list.Count(); i++)
                {
                    var client = list.ElementAt(i);
                    Signalling.SendPacketAsync(Packets.Signalling.Unmute.Create(participant.Key), client.Value.SignallingSocket);
                }
            }
        }

        private void SignallingDeafen(Packets.Signalling.Deafen packet, Socket socket)
        {
            var participant = Participants.FirstOrDefault(x => x.Value.SignallingSocket == socket);
            if (participant.Value != null && !participant.Value.IsDeafened)
            {
                participant.Value.LastActive = DateTime.UtcNow;
                participant.Value.IsDeafened = true;
                if (!participant.Value.Binded) return; //Return if not binded because the participants is not on other clients.

                var list = Participants.Where(x => x.Key != participant.Key && x.Value.Binded && x.Value.Channel == participant.Value.Channel);
                for (ushort i = 0; i < list.Count(); i++)
                {
                    var client = list.ElementAt(i);
                    Signalling.SendPacketAsync(Packets.Signalling.Deafen.Create(participant.Key), client.Value.SignallingSocket);
                }
            }
        }

        private void SignallingUndeafen(Packets.Signalling.Undeafen packet, Socket socket)
        {
            var participant = Participants.FirstOrDefault(x => x.Value.SignallingSocket == socket);
            if (participant.Value != null && participant.Value.IsDeafened)
            {
                participant.Value.LastActive = DateTime.UtcNow;
                participant.Value.IsDeafened = false;
                if (!participant.Value.Binded) return; //Return if not binded because the participants is not on other clients.

                var list = Participants.Where(x => x.Key != participant.Key && x.Value.Binded && x.Value.Channel == participant.Value.Channel);
                for (ushort i = 0; i < list.Count(); i++)
                {
                    var client = list.ElementAt(i);
                    Signalling.SendPacketAsync(Packets.Signalling.Undeafen.Create(participant.Key), client.Value.SignallingSocket);
                }
            }
        }

        private void SignallingJoinChannel(Packets.Signalling.JoinChannel packet, Socket socket)
        {
            var participant = Participants.FirstOrDefault(x => x.Value.SignallingSocket == socket);
            if (participant.Value != null && participant.Value.Binded)
            {
                participant.Value.LastActive = DateTime.UtcNow;
                var channel = ServerProperties.Channels.ElementAtOrDefault(packet.ChannelId - 1);
                if(participant.Value.Channel != packet.ChannelId && (channel.Password == packet.Password || string.IsNullOrWhiteSpace(channel.Password)))
                {
                    if(participant.Value.Channel != 0)
                        Signalling.SendPacketAsync(Packets.Signalling.LeaveChannel.Create(participant.Value.Channel), socket); //Tell the client to leave the previous channel.

                    var channelList = Participants.Where(x => x.Key != participant.Key && x.Value.Binded && x.Value.Channel == participant.Value.Channel);
                    for (ushort i = 0; i < channelList.Count(); i++)
                    {
                        var client = channelList.ElementAt(i);
                        Signalling.SendPacketAsync(Packets.Signalling.Logout.Create(participant.Key), client.Value.SignallingSocket);
                        Signalling.SendPacketAsync(Packets.Signalling.Logout.Create(client.Key), socket);
                    }

                    participant.Value.Channel = packet.ChannelId;

                    channelList = Participants.Where(x => x.Key != participant.Key && x.Value.Binded && x.Value.Channel == participant.Value.Channel);
                    for (ushort i = 0; i < channelList.Count(); i++)
                    {
                        var client = channelList.ElementAt(i);
                        Signalling.SendPacketAsync(Packets.Signalling.Login.Create(PositioningTypes.ServerSided, participant.Key, participant.Value.IsDeafened, participant.Value.IsMuted, participant.Value.Name, string.Empty), client.Value.SignallingSocket);
                        Signalling.SendPacketAsync(Packets.Signalling.Login.Create(PositioningTypes.ServerSided, client.Key, client.Value.IsDeafened, client.Value.IsMuted, client.Value.Name, string.Empty), socket);
                    }

                    Signalling.SendPacketAsync(Packets.Signalling.JoinChannel.Create(packet.ChannelId, string.Empty), socket);
                }
                else if(channel.Password != packet.Password || string.IsNullOrWhiteSpace(channel.Password))
                {
                    Signalling.SendPacketAsync(Packets.Signalling.Deny.Create("Incorrect Password!", false), socket);
                }
                else
                {
                    Signalling.SendPacketAsync(Packets.Signalling.Deny.Create("Channel does not exist!", false), socket);
                }
            }
        }

        private void SignallingLeaveChannel(Packets.Signalling.LeaveChannel packet, Socket socket)
        {
            var participant = Participants.FirstOrDefault(x => x.Value.SignallingSocket == socket);
            if (participant.Value != null && participant.Value.Binded && packet.ChannelId == participant.Value.Channel)
            {
                participant.Value.LastActive = DateTime.UtcNow;
                var channelList = Participants.Where(x => x.Key != participant.Key && x.Value.Binded && x.Value.Channel == participant.Value.Channel);
                for (ushort i = 0; i < channelList.Count(); i++)
                {
                    var client = channelList.ElementAt(i);
                    Signalling.SendPacketAsync(Packets.Signalling.Logout.Create(participant.Key), client.Value.SignallingSocket);
                    Signalling.SendPacketAsync(Packets.Signalling.Logout.Create(client.Key), socket);
                }

                participant.Value.Channel = 0;
                channelList = Participants.Where(x => x.Key != participant.Key && x.Value.Binded && x.Value.Channel == participant.Value.Channel);
                for (ushort i = 0; i < channelList.Count(); i++)
                {
                    var client = channelList.ElementAt(i);
                    Signalling.SendPacketAsync(Packets.Signalling.Login.Create(PositioningTypes.ServerSided, participant.Key, participant.Value.IsDeafened, participant.Value.IsMuted, participant.Value.Name, string.Empty), client.Value.SignallingSocket);
                    Signalling.SendPacketAsync(Packets.Signalling.Login.Create(PositioningTypes.ServerSided, client.Key, client.Value.IsDeafened, client.Value.IsMuted, client.Value.Name, string.Empty), socket);
                }

                Signalling.SendPacketAsync(Packets.Signalling.LeaveChannel.Create(packet.ChannelId), socket);
            }
        }

        private void SignallingSocketConnected(Socket socket, NetworkStream stream)
        {
            //Wait 5 seconds before checking if the socket is used.
            Task.Delay(5000).ContinueWith(t => {
                var participant = Participants.FirstOrDefault(x => x.Value.SignallingSocket == socket);
                if(participant.Value == null)
                {
                    //If client closed the socket, We catch it otherwise we close the connection.
                    try
                    {
                        socket.Disconnect(false);
                        stream.Close();
                        socket.Close();
                    }
                    catch (ObjectDisposedException)
                    {
                        return;
                    }
                }
            });
        }

        private void SignallingSocketDisconnected(Socket socket, string reason)
        {
            var participant = Participants.FirstOrDefault(x => x.Value.SignallingSocket == socket);
            RemoveParticipant(participant, true, reason);
        }

        private void SignallingPingCheck(Packets.Signalling.PingCheck packet, Socket socket)
        {
            var participant = Participants.FirstOrDefault(x => x.Value.SignallingSocket == socket);
            if (participant.Value != null)
            {
                participant.Value.LastActive = DateTime.UtcNow;

                Signalling.SendPacketAsync(Packets.Signalling.PingCheck.Create(), socket);
            }
        }
        #endregion

        #region Voice
        private void VoiceSocketStarted()
        {
            OnVoiceStarted?.Invoke();
            if (ServerProperties.ConnectionType == ConnectionTypes.Server || ServerProperties.ConnectionType == ConnectionTypes.Hybrid)
            {
                _ = Task.Run(() =>
                {
                    try
                    {
                        MCComm.LogInbound = ServerProperties.Debugger.LogInboundMCCommPackets;
                        MCComm.LogOutbound = ServerProperties.Debugger.LogOutboundMCCommPackets;
                        MCComm.InboundFilter = ServerProperties.Debugger.InboundMCCommFilter;
                        MCComm.OutboundFilter = ServerProperties.Debugger.OutboundMCCommFilter;
                        MCComm.LogExceptions = ServerProperties.Debugger.LogExceptions;
                        MCComm.Start(ServerProperties.MCCommPortTCP, ServerProperties.PermanentServerKey);
                    }
                    catch (Exception ex)
                    {
                        OnError?.Invoke(ex);
                        CTS.Cancel();
                        /*
                        var username = Environment.GetEnvironmentVariable("USERNAME");
                        var userdomain = Environment.GetEnvironmentVariable("USERDOMAIN");
                        Console.WriteLine($"Please give access by typing in the following command in a command prompt\nnetsh http add urlacl url=http://*:{Port}/ user={userdomain}\\{username} listen=yes\nAnd then restart the server\n");
                        */
                    }
                });
            }
        }

        private void OnVoiceLogin(Packets.Voice.Login packet, EndPoint endPoint)
        {
            var participant = Participants.FirstOrDefault(x => x.Value.SignallingSocket.RemoteEndPoint.ToString()?.Split(':').FirstOrDefault() == endPoint.ToString()?.Split(':').FirstOrDefault() && x.Key == packet.LoginKey);
            if (participant.Value != null && participant.Value.VoiceEndpoint == null)
            {
                participant.Value.VoiceEndpoint = endPoint;
                Voice.SendPacketAsync(Packets.Voice.Accept.Create(), endPoint);
                OnParticipantConnected?.Invoke(participant.Value, participant.Key);
            }
            else
            {
                Voice.SendPacketAsync(Packets.Voice.Deny.Create("Key is invalid or used!"), endPoint);
            }
        }

        private void VoiceClientAudio(Packets.Voice.ClientAudio packet, EndPoint endPoint)
        {
            _ = Task.Run(() =>
            {
                var participant = Participants.FirstOrDefault(x => x.Value.VoiceEndpoint?.ToString() == endPoint.ToString());
                if (participant.Value != null &&
                    !participant.Value.IsMuted && !participant.Value.IsDeafened &&
                    !participant.Value.IsServerMuted && participant.Value.Binded)
                {
                    if (ServerProperties.ProximityToggle)
                    {
                        if (participant.Value.IsDead || string.IsNullOrWhiteSpace(participant.Value.EnvironmentId)) return;

                        var list = Participants.Where(x =>
                        x.Key != participant.Key &&
                        x.Value.Binded &&
                        !x.Value.IsDeafened &&
                        !x.Value.IsDead &&
                        x.Value.Channel == participant.Value.Channel &&
                        !string.IsNullOrWhiteSpace(x.Value.EnvironmentId) &&
                        x.Value.EnvironmentId == participant.Value.EnvironmentId &&
                        Vector3.Distance(x.Value.Position, participant.Value.Position) <= ServerProperties.ProximityDistance);

                        for (ushort i = 0; i < list.Count(); i++)
                        {
                            var client = list.ElementAt(i);

                            if (client.Value.VoiceEndpoint != null)
                            {
                                var volume = 1.0f - Math.Clamp(Vector3.Distance(client.Value.Position, participant.Value.Position) / ServerProperties.ProximityDistance, 0.0f, 1.0f);
                                var echo = ServerProperties.VoiceEffects ? Math.Max(participant.Value.CaveDensity, client.Value.CaveDensity) * (1.0f - volume) : 0.0f;
                                var muffled = ServerProperties.VoiceEffects && (client.Value.InWater || participant.Value.InWater);
                                var rotation = (float)(Math.Atan2(client.Value.Position.Z - participant.Value.Position.Z, client.Value.Position.X - participant.Value.Position.X) - (client.Value.Rotation * Math.PI / 180));

                                Voice.SendPacketAsync(Packets.Voice.ServerAudio.Create(participant.Key, packet.PacketCount, volume, echo, rotation, muffled, packet.Audio), client.Value.VoiceEndpoint);
                            }
                        }
                    }
                    else
                    {
                        var list = Participants.Where(x =>
                        x.Key != participant.Key &&
                        x.Value.Binded &&
                        x.Value.Channel == participant.Value.Channel);

                        for (ushort i = 0; i < list.Count(); i++)
                        {
                            var client = list.ElementAt(i);

                            if (client.Value.VoiceEndpoint != null)
                            {
                                Voice.SendPacketAsync(Packets.Voice.ServerAudio.Create(participant.Key, packet.PacketCount, 1.0f, 0.0f, 1.5f, false, packet.Audio), client.Value.VoiceEndpoint);
                            }
                        }
                    }
                }
            });
        }

        private void VoiceUpdatePosition(Packets.Voice.UpdatePosition packet, EndPoint endPoint)
        {
            var participant = Participants.FirstOrDefault(x => x.Value.VoiceEndpoint?.ToString() == endPoint.ToString());
            if(participant.Value != null && participant.Value.Binded && participant.Value.PositioningType == PositioningTypes.ClientSided)
            {
                participant.Value.EnvironmentId = packet.EnvironmentId;
                participant.Value.Position = packet.Position;
            }
        }
        #endregion

        #region MCComm
        //MCComm
        private void MCCommStarted()
        {
            OnWebserverStarted?.Invoke();
        }

        private void MCCommLogin(Packets.MCComm.Login packet, HttpListenerContext ctx)
        {
            if(packet.LoginKey != MCComm.ServerKey)
            {
                var denyPacket = Packets.MCComm.Deny.Create("Invalid Key!");
                MCComm.SendResponse(ctx, HttpStatusCode.OK, denyPacket);
                return;
            }
            if(ExternalServers.Exists(x => x.IP == ctx.Request.RemoteEndPoint?.ToString().Split(":").FirstOrDefault()))
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
            Signalling.SendPacketAsync(Packets.Signalling.Binded.Create(participant.Value.Name), participant.Value.SignallingSocket);

            MCComm.SendResponse(ctx, HttpStatusCode.OK, Packets.MCComm.Accept.Create());
            var list = Participants.Where(x => x.Key != participant.Key && x.Value.Binded && x.Value.Channel == participant.Value.Channel);
            for (ushort i = 0; i < list.Count(); i++)
            {
                var client = list.ElementAt(i);
                Signalling.SendPacketAsync(Packets.Signalling.Login.Create(PositioningTypes.ServerSided, client.Key, client.Value.IsDeafened, client.Value.IsMuted, client.Value.Name, string.Empty), participant.Value.SignallingSocket);
                Signalling.SendPacketAsync(Packets.Signalling.Login.Create(PositioningTypes.ServerSided, participant.Key, participant.Value.IsDeafened, participant.Value.IsMuted, participant.Value.Name, string.Empty), client.Value.SignallingSocket);
            }

            var channelList = ServerProperties.Channels;
            for(int i = 0; i < channelList.Count; i++)
            {
                var channel = ServerProperties.Channels[i];
                Signalling.SendPacketAsync(Packets.Signalling.AddChannel.Create(channel.Name, (byte)(i + 1), !string.IsNullOrWhiteSpace(channel.Password)), participant.Value.SignallingSocket);
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
                if(participant.Value != null)
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

                    if(participant.Value.InWater != player.InWater)
                        participant.Value.InWater = player.InWater;
                }
            }

            MCComm.SendResponse(ctx, HttpStatusCode.OK, Packets.MCComm.Accept.Create());
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
                MCComm.SendResponse(ctx, HttpStatusCode.NotAcceptable, Packets.MCComm.Deny.Create("Proximity distance must be between 1 and 120!"));
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
                RemoveParticipant(participant, true, "MCComm server kicked.");
                OnParticipantDisconnected?.Invoke("MCComm server kicked.", participant.Value, participant.Key);
                return;
            }

            MCComm.SendResponse(ctx, HttpStatusCode.NotFound, Packets.MCComm.Deny.Create("Could not find participant!"));
        }

        private bool ServerLoggedIn(HttpListenerContext ctx)
        {
            var server = ExternalServers.FirstOrDefault(x => x.IP == ctx.Request.RemoteEndPoint?.ToString().Split(":").FirstOrDefault());
            if (server == null)
            {
                var denyPacket = Packets.MCComm.Deny.Create("Not Logged In!");
                MCComm.SendResponse(ctx, HttpStatusCode.Forbidden, denyPacket);
                return false;
            }
            else
            {
                server.LastActive = DateTime.UtcNow;
                return true;
            }
        }
        #endregion

        #region Public Methods
        public bool RemoveParticipant(ushort key, bool broadcast = true, string? reason = null)
        {
            var participant = Participants.FirstOrDefault(x => x.Key == key);
            if(participant.Value != null)
            {
                return RemoveParticipant(participant, broadcast, reason);
            }
            return false;
        }

        public bool RemoveParticipant(KeyValuePair<ushort, VoiceCraftParticipant> participant, bool broadcast = true, string? reason = null)
        {
            if (participant.Value != null)
            {
                Participants.TryRemove(participant.Key, out _);
                OnParticipantDisconnected?.Invoke(reason ?? "No Reason", participant.Value, participant.Key);

                if (broadcast)
                {
                    Signalling.SendPacketAsync(Packets.Signalling.Logout.Create(participant.Key), participant.Value.SignallingSocket);
                    foreach (var client in Participants)
                    {
                        if(client.Value.Channel == participant.Value.Channel)
                            Signalling.SendPacketAsync(Packets.Signalling.Logout.Create(participant.Key), client.Value.SignallingSocket);
                    }
                }
                return true;
            }
            return false;
        }
        #endregion
    }
}