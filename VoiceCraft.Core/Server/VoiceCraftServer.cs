using System;
using System.Collections.Concurrent;
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
        public const string Version = "v1.0.0";

        //Data
        public ConcurrentDictionary<ushort, VoiceCraftParticipant> Participants = new ConcurrentDictionary<ushort, VoiceCraftParticipant>();
        public Properties ServerProperties { get; set; } = new Properties();
        public Banlist Banlist { get; set; } = new Banlist();
        private CancellationTokenSource CTS { get; } = new CancellationTokenSource();

        //Sockets
        public SignallingSocket Signalling { get; set; }
        public VoiceSocket Voice { get; set; }
        public MCCommSocket MCComm { get; set; }

        //Delegates
        public delegate void SignallingStarted();
        public delegate void VoiceStarted();
        public delegate void WebserverStarted();
        public delegate void ParticipantConnected(VoiceCraftParticipant participant, ushort key);
        public delegate void ParticipantBinded(VoiceCraftParticipant participant, ushort key);
        public delegate void ParticipantUnbinded(VoiceCraftParticipant participant, ushort key);
        public delegate void ParticipantDisconnected(string reason, VoiceCraftParticipant participant, ushort key);
        public delegate void Error(Exception exception);

        //Events
        public event SignallingStarted? OnSignallingStarted;
        public event VoiceStarted? OnVoiceStarted;
        public event WebserverStarted? OnWebserverStarted;
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

            //Event methods in order!
            Signalling.OnLoginPacketReceived += SignallingLogin;
            Voice.OnLoginPacketReceived += OnVoiceLogin;
            Signalling.OnBindedPacketReceived += SignallingBinded;
            Signalling.OnUnbindedPacketReceived += SignallingUnbinded;
            MCComm.OnBindedPacketReceived += MCCommBinded;
            Voice.OnClientAudioPacketReceived += VoiceClientAudio;
            Signalling.OnMutePacketReceived += SignallingMute;
            Signalling.OnUnmutePacketReceived += SignallingUnmute;
            Signalling.OnDeafenPacketReceived += SignallingDeafen;
            Signalling.OnUndeafenPacketReceived += SignallingUndeafen;

            //Ping Packet
            Signalling.OnPingPacketReceived += PingReceived;
            Signalling.OnSocketDisconnected += SignallingSocketDisconnected;
        }

        public void Start()
        {
            _ = Task.Run(() =>
            {
                try
                {
                    Signalling.Start(ServerProperties.SignallingPortTCP);
                }
                catch (Exception ex)
                {
                    OnError?.Invoke(ex);
                    CTS.Cancel();
                }
            });
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

            Signalling.SendPacket(new SignallingPacket()
            {
                PacketType = SignallingPacketTypes.Ping,
                PacketData = new Packets.Signalling.Ping()
                {
                    ServerData =
                    $"MOTD: {ServerProperties.ServerMOTD}" +
                    $"\nConnection Type: {connType}" +
                    $"\nConnected Participants: {Participants.Count}"
                }
            }, socket);
        }

        private void SignallingSocketStarted()
        {
            OnSignallingStarted?.Invoke();
            _ = Task.Run(() =>
            {
                try
                {
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
                Signalling.SendPacketAsync(new SignallingPacket()
                {
                    PacketType = SignallingPacketTypes.Deny,
                    PacketData = new Packets.Signalling.Deny() { Reason = "Versions do not match!" }
                }, socket);
                return;
            }
            if (Banlist.IPBans.Exists(x => x == socket.RemoteEndPoint.ToString()?.Split(':').FirstOrDefault()))
            {
                Signalling.SendPacketAsync(new SignallingPacket()
                {
                    PacketType = SignallingPacketTypes.Deny,
                    PacketData = new Packets.Signalling.Deny() { Reason = "You have been banned from the server!" }
                }, socket);
                return;
            }

            if (packet.PositioningType != PositioningTypes.ClientSided &&
                (ServerProperties.ConnectionType == ConnectionTypes.Client || ServerProperties.ConnectionType == ConnectionTypes.Hybrid))
            {
                Signalling.SendPacketAsync(new SignallingPacket()
                {
                    PacketType = SignallingPacketTypes.Deny,
                    PacketData = new Packets.Signalling.Deny()
                    {
                        Reason = "Server only accepts client sided positioning!"
                    }
                }, socket);
                return;
            }
            else if (packet.PositioningType != PositioningTypes.ServerSided &&
                (ServerProperties.ConnectionType == ConnectionTypes.Server || ServerProperties.ConnectionType == ConnectionTypes.Hybrid))
            {
                Signalling.SendPacketAsync(new SignallingPacket()
                {
                    PacketType = SignallingPacketTypes.Deny,
                    PacketData = new Packets.Signalling.Deny()
                    {
                        Reason = "Server only accepts server sided positioning!"
                    }
                }, socket);
                return;
            }
            var key = packet.LoginKey;
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
            Signalling.SendPacketAsync(new SignallingPacket()
            {
                PacketType = SignallingPacketTypes.Accept,
                PacketData = new Packets.Signalling.Accept()
                {
                    LoginKey = key,
                    VoicePort = ServerProperties.VoicePortUDP
                }
            }, socket);
        }

        private void SignallingBinded(Packets.Signalling.Binded packet, Socket socket)
        {
            var participant = Participants.FirstOrDefault(x => x.Value.SignallingSocket == socket);
            if (participant.Value != null && participant.Value.PositioningType == PositioningTypes.ClientSided && !participant.Value.Binded)
            {
                participant.Value.Binded = true;
                participant.Value.Name = packet.Name;
                var list = Participants.Where(x => x.Key != participant.Key && x.Value.Binded);
                for (ushort i = 0; i < list.Count(); i++)
                {
                    var client = list.ElementAt(i);
                    Signalling.SendPacketAsync(new SignallingPacket()
                    {
                        PacketType = SignallingPacketTypes.Login,
                        PacketData = new Packets.Signalling.Login()
                        {
                            LoginKey = client.Key,
                            Name = client.Value.Name,
                            IsDeafened = client.Value.IsDeafened,
                            IsMuted = client.Value.IsMuted
                        }
                    }, participant.Value.SignallingSocket);

                    Signalling.SendPacketAsync(new SignallingPacket()
                    {
                        PacketType = SignallingPacketTypes.Login,
                        PacketData = new Packets.Signalling.Login()
                        {
                            LoginKey = participant.Key,
                            Name = participant.Value.Name,
                            IsDeafened = participant.Value.IsDeafened,
                            IsMuted = participant.Value.IsMuted
                        }
                    }, client.Value.SignallingSocket);
                }
                OnParticipantBinded?.Invoke(participant.Value, participant.Key);
            }
        }

        private void SignallingUnbinded(Packets.Signalling.Unbinded packet, Socket socket)
        {
            var participant = Participants.FirstOrDefault(x => x.Value.SignallingSocket == socket);
            if (participant.Value != null && participant.Value.PositioningType == PositioningTypes.ClientSided && participant.Value.Binded)
            {
                participant.Value.Binded = false;
                var list = Participants.Where(x => x.Key != participant.Key && x.Value.Binded);
                for (ushort i = 0; i < list.Count(); i++)
                {
                    var client = list.ElementAt(i);
                    //Logout the unbinded participant from all other clients.
                    Signalling.SendPacketAsync(new SignallingPacket()
                    {
                        PacketType = SignallingPacketTypes.Logout,
                        PacketData = new Packets.Signalling.Logout()
                        {
                            LoginKey = participant.Key
                        }
                    }, client.Value.SignallingSocket);
                }
                OnParticipantUnbinded?.Invoke(participant.Value, participant.Key);
            }
        }

        private void SignallingMute(Packets.Signalling.Mute packet, Socket socket)
        {
            var participant = Participants.FirstOrDefault(x => x.Value.SignallingSocket == socket);
            if (participant.Value != null && !participant.Value.IsMuted)
            {
                participant.Value.IsMuted = true;
                if (!participant.Value.Binded) return; //Return if not binded because the participants is not on other clients.

                var list = Participants.Where(x => x.Key != participant.Key && x.Value.Binded);
                for (ushort i = 0; i < list.Count(); i++)
                {
                    var client = list.ElementAt(i);
                    Signalling.SendPacketAsync(new SignallingPacket()
                    {
                        PacketType = SignallingPacketTypes.Mute,
                        PacketData = new Packets.Signalling.Mute()
                        {
                            LoginKey = participant.Key
                        }
                    }, client.Value.SignallingSocket);
                }
            }
        }

        private void SignallingUnmute(Packets.Signalling.Unmute packet, Socket socket)
        {
            var participant = Participants.FirstOrDefault(x => x.Value.SignallingSocket == socket);
            if (participant.Value != null && participant.Value.IsMuted)
            {
                participant.Value.IsMuted = false;
                if (!participant.Value.Binded) return; //Return if not binded because the participants is not on other clients.

                var list = Participants.Where(x => x.Key != participant.Key && x.Value.Binded);
                for (ushort i = 0; i < list.Count(); i++)
                {
                    var client = list.ElementAt(i);
                    Signalling.SendPacketAsync(new SignallingPacket()
                    {
                        PacketType = SignallingPacketTypes.Unmute,
                        PacketData = new Packets.Signalling.Unmute()
                        {
                            LoginKey = participant.Key
                        }
                    }, client.Value.SignallingSocket);
                }
            }
        }

        private void SignallingDeafen(Packets.Signalling.Deafen packet, Socket socket)
        {
            var participant = Participants.FirstOrDefault(x => x.Value.SignallingSocket == socket);
            if (participant.Value != null && !participant.Value.IsDeafened)
            {
                participant.Value.IsDeafened = true;
                if (!participant.Value.Binded) return; //Return if not binded because the participants is not on other clients.

                var list = Participants.Where(x => x.Key != participant.Key && x.Value.Binded);
                for (ushort i = 0; i < list.Count(); i++)
                {
                    var client = list.ElementAt(i);
                    Signalling.SendPacketAsync(new SignallingPacket()
                    {
                        PacketType = SignallingPacketTypes.Deafen,
                        PacketData = new Packets.Signalling.Deafen()
                        {
                            LoginKey = participant.Key
                        }
                    }, client.Value.SignallingSocket);
                }
            }
        }

        private void SignallingUndeafen(Packets.Signalling.Undeafen packet, Socket socket)
        {
            var participant = Participants.FirstOrDefault(x => x.Value.SignallingSocket == socket);
            if (participant.Value != null && participant.Value.IsDeafened)
            {
                participant.Value.IsDeafened = false;
                if (!participant.Value.Binded) return; //Return if not binded because the participants is not on other clients.

                var list = Participants.Where(x => x.Key != participant.Key && x.Value.Binded);
                for (ushort i = 0; i < list.Count(); i++)
                {
                    var client = list.ElementAt(i);
                    Signalling.SendPacketAsync(new SignallingPacket()
                    {
                        PacketType = SignallingPacketTypes.Undeafen,
                        PacketData = new Packets.Signalling.Undeafen()
                        {
                            LoginKey = participant.Key
                        }
                    }, client.Value.SignallingSocket);
                }
            }
        }

        private void SignallingSocketDisconnected(Socket socket, string reason)
        {
            var participant = Participants.FirstOrDefault(x => x.Value.SignallingSocket == socket);
            if (participant.Value != null)
            {
                Participants.TryRemove(participant.Key, out _);
                OnParticipantDisconnected?.Invoke(reason, participant.Value, participant.Key);
                var list = Participants.Where(x => x.Key != participant.Key);
                for (ushort i = 0; i < list.Count(); i++)
                {
                    var client = list.ElementAt(i);
                    Signalling.SendPacketAsync(new SignallingPacket()
                    {
                        PacketType = SignallingPacketTypes.Logout,
                        PacketData = new Packets.Signalling.Logout()
                        {
                            LoginKey = participant.Key
                        }
                    }, client.Value.SignallingSocket);
                }
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
                Voice.SendPacketAsync(new VoicePacket()
                {
                    PacketType = VoicePacketTypes.Accept,
                    PacketData = new Packets.Voice.Accept()
                }, endPoint);
                OnParticipantConnected?.Invoke(participant.Value, participant.Key);
            }
            else
            {
                Voice.SendPacketAsync(new VoicePacket()
                {
                    PacketType = VoicePacketTypes.Deny,
                    PacketData = new Packets.Voice.Deny()
                    {
                        Reason = "Key is invalid or used!"
                    }
                }, endPoint);
            }
        }

        private void VoiceClientAudio(Packets.Voice.ClientAudio packet, EndPoint endPoint)
        {
            var participant = Participants.FirstOrDefault(x => x.Value.VoiceEndpoint?.ToString() == endPoint.ToString());
            if (participant.Value != null && 
                !participant.Value.IsMuted && participant.Value.IsDeafened && 
                !participant.Value.IsServerMuted && participant.Value.Binded &&
                !string.IsNullOrWhiteSpace(participant.Value.EnvironmentId))
            {
                if (ServerProperties.ProximityToggle && !participant.Value.IsDead)
                {
                    var list = Participants.Where(x =>
                    x.Key != participant.Key &&
                    x.Value.Binded &&
                    !x.Value.IsDeafened &&
                    !x.Value.IsDead &&
                    !string.IsNullOrWhiteSpace(x.Value.EnvironmentId) &&
                    x.Value.EnvironmentId == participant.Value.EnvironmentId &&
                    Vector3.Distance(x.Value.Position, participant.Value.Position) <= ServerProperties.ProximityDistance);

                    for (ushort i = 0; i < list.Count(); i++)
                    {
                        var client = list.ElementAt(i);

                        if (client.Value.VoiceEndpoint != null)
                        {
                            var volume = Math.Clamp(Vector3.Distance(client.Value.Position, participant.Value.Position) / ServerProperties.ProximityDistance, 0.0f, 1.0f);
                            Voice.SendPacketAsync(new VoicePacket()
                            {
                                PacketType = VoicePacketTypes.ServerAudio,
                                PacketData = new Packets.Voice.ServerAudio()
                                {
                                    Audio = packet.Audio,
                                    LoginKey = participant.Key,
                                    PacketCount = packet.PacketCount,
                                    Volume = volume,
                                    EchoFactor = ServerProperties.VoiceEffects? participant.Value.CaveDensity * volume : 0.0f
                                }
                            }, client.Value.VoiceEndpoint);
                        }
                    }
                }
                else
                {
                    var list = Participants.Where(x =>
                    x.Key != participant.Key &&
                    x.Value.Binded);

                    for (ushort i = 0; i < list.Count(); i++)
                    {
                        var client = list.ElementAt(i);

                        if (client.Value.VoiceEndpoint != null)
                        {
                            Voice.SendPacketAsync(new VoicePacket()
                            {
                                PacketType = VoicePacketTypes.ServerAudio,
                                PacketData = new Packets.Voice.ServerAudio()
                                {
                                    Audio = packet.Audio,
                                    LoginKey = participant.Key,
                                    PacketCount = packet.PacketCount,
                                    Volume = 1.0f,
                                    EchoFactor = 0.0f,
                                    Rotation = 0.0f
                                }
                            }, client.Value.VoiceEndpoint);
                        }
                    }
                }
            }
        }
        #endregion

        #region MCComm
        //MCComm
        private void MCCommStarted()
        {
            OnWebserverStarted?.Invoke();
        }

        private void MCCommBinded(WebserverPacket packet, HttpListenerContext ctx)
        {
            var participant = Participants.FirstOrDefault(x => x.Key == packet.PlayerKey);
            if (participant.Value == null)
            {
                MCComm.SendResponse(ctx, HttpStatusCode.NotFound, "Error. Could not find key!");
                return;
            }
            if (participant.Value.Binded)
            {
                MCComm.SendResponse(ctx, HttpStatusCode.Conflict, "Error. Key has already been binded to a participant.");
                return;
            }
            if (Participants.FirstOrDefault(x => x.Value.MinecraftId == packet.PlayerId).Value != null)
            {
                MCComm.SendResponse(ctx, HttpStatusCode.Conflict, "Error. PlayerId is already binded to a participant!");
                return;
            }
            if (participant.Value.PositioningType == PositioningTypes.ClientSided)
            {
                MCComm.SendResponse(ctx, HttpStatusCode.Forbidden, "Error. Participant is using client sided positioning.");
                return;
            }

            participant.Value.Name = packet.Gamertag;
            participant.Value.MinecraftId = packet.PlayerId;
            participant.Value.Binded = true;
            Signalling.SendPacketAsync(new SignallingPacket()
            {
                PacketType = SignallingPacketTypes.Binded,
                PacketData = new Packets.Signalling.Binded()
                {
                    Name = participant.Value.Name
                }
            }, participant.Value.SignallingSocket);
            MCComm.SendResponse(ctx, HttpStatusCode.Accepted, "Successfully Binded");
            var list = Participants.Where(x => x.Key != participant.Key && x.Value.Binded);
            for (ushort i = 0; i < list.Count(); i++)
            {
                var client = list.ElementAt(i);
                Signalling.SendPacketAsync(new SignallingPacket()
                {
                    PacketType = SignallingPacketTypes.Login,
                    PacketData = new Packets.Signalling.Login()
                    {
                        LoginKey = client.Key,
                        Name = client.Value.Name,
                        IsDeafened = client.Value.IsDeafened,
                        IsMuted = client.Value.IsMuted
                    }
                }, participant.Value.SignallingSocket);

                Signalling.SendPacketAsync(new SignallingPacket()
                {
                    PacketType = SignallingPacketTypes.Login,
                    PacketData = new Packets.Signalling.Login()
                    {
                        LoginKey = participant.Key,
                        Name = participant.Value.Name,
                        IsDeafened = participant.Value.IsDeafened,
                        IsMuted = participant.Value.IsMuted
                    }
                }, client.Value.SignallingSocket);
            }

            OnParticipantBinded?.Invoke(participant.Value, participant.Key);
        }
        #endregion
    }
}
