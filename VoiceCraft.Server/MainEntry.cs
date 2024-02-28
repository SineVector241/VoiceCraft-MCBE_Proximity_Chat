using Newtonsoft.Json;
using System.Net.Sockets;
using VoiceCraft.Core.Server;
using VoiceCraft.Data.Server;

namespace VoiceCraft.Server
{
    public class MainEntry
    {
        readonly VoiceCraftServer server = new VoiceCraftServer();
        readonly ServerData serverData = new ServerData();

        public MainEntry()
        {
            //Event Registrations
            server.OnError += ServerError;
            server.OnSignallingStarted += SignallingStarted;
            server.OnVoiceStarted += VoiceStarted;
            server.OnWebserverStarted += WebserverStarted;
            server.OnParticipantConnected += ParticipantConnected;
            server.OnParticipantBinded += ParticipantBinded;
            server.OnParticipantUnbinded += ParticipantUnbinded;
            server.OnParticipantDisconnected += ParticipantDisconnected;
            server.OnExternalServerConnected += ExternalServerConnected;
            server.OnExternalServerDisconnected += ExternalServerDisconnected;
            server.OnExceptionError += ExceptionError;

            server.Signalling.OnOutboundPacket += SignallingOutbound;
            server.Signalling.OnInboundPacket += SignallingInbound;
            server.Signalling.OnExceptionError += ExceptionError;
            server.Voice.OnOutboundPacket += VoiceOutbound;
            server.Voice.OnInboundPacket += VoiceInbound;
            server.Voice.OnExceptionError += ExceptionError;
            server.MCComm.OnInboundPacket += MCCommInbound;
            server.MCComm.OnOutboundPacket += MCCommOutbound;
            server.MCComm.OnExceptionError += ExceptionError;
        }

        public async Task Start()
        {
            Console.Title = $"VoiceCraft - {VoiceCraftServer.Version}: Starting...";
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(@"__     __    _           ____            __ _");
            Console.WriteLine(@"\ \   / /__ (_) ___ ___ / ___|_ __ __ _ / _| |_");
            Console.WriteLine(@" \ \ / / _ \| |/ __/ _ \ |   | '__/ _` | |_| __|");
            Console.WriteLine(@"  \ V / (_) | | (_|  __/ |___| | | (_| |  _| |_");
            Console.WriteLine(@"   \_/ \___/|_|\___\___|\____|_|  \__,_|_|  \__|");
#if DEBUG
            Console.WriteLine($"[v1.0.1][{VoiceCraftServer.Version}]=========================[DEBUG]\n");
#else
            Console.WriteLine($"[v1.0.1][{VoiceCraftServer.Version}]=======================[RELEASE]\n");
#endif

            //Register Commands
            CommandHandler.RegisterCommand("help", HelpCommand);
            CommandHandler.RegisterCommand("exit", ExitCommand);
            CommandHandler.RegisterCommand("list", ListCommand);
            CommandHandler.RegisterCommand("mute", MuteCommand);
            CommandHandler.RegisterCommand("unmute", UnmuteCommand);
            CommandHandler.RegisterCommand("kick", KickCommand);
            CommandHandler.RegisterCommand("ban", BanCommand);
            CommandHandler.RegisterCommand("unban", UnbanCommand);
            CommandHandler.RegisterCommand("banlist", BanlistCommand);
            CommandHandler.RegisterCommand("setproximity", SetProximityCommand);
            CommandHandler.RegisterCommand("toggleproximity", ToggleProximityCommand);
            CommandHandler.RegisterCommand("setmotd", SetMotdCommand);
            CommandHandler.RegisterCommand("toggleeffects", ToggleEffectsCommand);
            CommandHandler.RegisterCommand("debug", DebugCommand);

            try
            {
                server.ServerProperties = serverData.LoadProperties();
                server.Banlist = serverData.LoadBanlist();
                server.Start();

                while (true)
                {
                    try
                    {
                        var input = Console.ReadLine();
                        if (!string.IsNullOrWhiteSpace(input))
                            CommandHandler.ParseCommand(input.ToLower());
                    }
                    catch (Exception ex)
                    {
#if DEBUG
                        Logger.LogToConsole(LogType.Error, ex.ToString(), "Commands");
#else
                        Logger.LogToConsole(LogType.Error, ex.Message.ToString(), "Socket");
#endif
                    }
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                Logger.LogToConsole(LogType.Error, ex.ToString(), "Server");
#else
                Logger.LogToConsole(LogType.Error, ex.Message.ToString(), "Socket");
#endif
                Logger.LogToConsole(LogType.Info, "Shutting down server in 10 seconds...", "Server");
                await Task.Delay(10000);
                ExitCommand(new string[0]);
            }
        }

        #region Event Methods
        private void ServerError(Exception ex)
        {
#if DEBUG
            Logger.LogToConsole(LogType.Error, ex.ToString(), "Socket");
#else
            Logger.LogToConsole(LogType.Error, ex.Message.ToString(), "Socket");
#endif
            Logger.LogToConsole(LogType.Info, "Shutting down server in 10 seconds...", "Server");
            Task.Delay(10000).Wait();
            ExitCommand(new string[0]);
        }

        private void SignallingStarted()
        {
            Logger.LogToConsole(LogType.Success, $"Signalling started - Port:{server.ServerProperties.SignallingPortTCP} TCP", "Socket");
        }

        private void VoiceStarted()
        {
            Logger.LogToConsole(LogType.Success, $"Voice started - Port:{server.ServerProperties.VoicePortUDP} UDP", "Socket");
            if (server.ServerProperties.ConnectionType == ConnectionTypes.Client)
            {
                foreach(var channel in server.ServerProperties.Channels)
                {
                    Logger.LogToConsole(LogType.Success, $"Channel Added - Name: {channel.Name}, Password?: {!string.IsNullOrWhiteSpace(channel.Password)}", "Channel");
                }

                Logger.LogToConsole(LogType.Success, "Server started!", "Server");
                Console.Title = $"VoiceCraft - {VoiceCraftServer.Version}: Running.";
            }
        }

        private void WebserverStarted()
        {
            Logger.LogToConsole(LogType.Success, $"MCComm started - Port:{server.ServerProperties.MCCommPortTCP} TCP", "Socket");

            foreach (var channel in server.ServerProperties.Channels)
            {
                Logger.LogToConsole(LogType.Success, $"Channel Added - Name: {channel.Name}, Password?: {!string.IsNullOrWhiteSpace(channel.Password)}", "Channel");
            }

            Logger.LogToConsole(LogType.Success, "Server started!", "Server");
            Logger.LogToConsole(LogType.Info, $"Server key: {server.ServerProperties.PermanentServerKey}", "Server");

            Console.Title = $"VoiceCraft - {VoiceCraftServer.Version}: Running.";
        }

        private void ExternalServerConnected(ExternalServer server)
        {
            Logger.LogToConsole(LogType.Success, $"External Server Connected: IP - {server.IP}", "Server");
        }

        private void ExternalServerDisconnected(ExternalServer server, string reason)
        {
            Logger.LogToConsole(LogType.Warn, $"External Server Disconnected: IP - {server.IP}, Reason - {reason}", "Server");
        }

        private void ParticipantConnected(VoiceCraftParticipant participant, ushort key)
        {
            Logger.LogToConsole(LogType.Success, $"Participant connected: Key - {key}, Positioning Type - {participant.PositioningType}", "Server");
        }

        private void ParticipantBinded(VoiceCraftParticipant participant, ushort key)
        {
            Logger.LogToConsole(LogType.Success, $"Participant binded: Key - {key}, Name - {participant.Name}", "Server");
        }

        private void ParticipantUnbinded(VoiceCraftParticipant participant, ushort key)
        {
            Logger.LogToConsole(LogType.Warn, $"Participant unbinded: Key - {key}, Name - {participant.Name}", "Server");
        }

        private void ParticipantDisconnected(string reason, VoiceCraftParticipant participant, ushort key)
        {
            Logger.LogToConsole(LogType.Warn, $"Participant disconnected: Key - {key}, Reason - {reason}", "Server");
        }

        //Debug Events
        private void SignallingOutbound(Core.Packets.Interfaces.ISignallingPacket packet, Socket socket)
        {
            Logger.LogToConsole(LogType.Info, JsonConvert.SerializeObject(packet), "DEBUG-SO");
        }

        private void SignallingInbound(Core.Packets.Interfaces.ISignallingPacket packet, Socket socket)
        {
            Logger.LogToConsole(LogType.Info, JsonConvert.SerializeObject(packet), "DEBUG-SI");
        }

        private void VoiceOutbound(Core.Packets.Interfaces.IVoicePacket packet, System.Net.EndPoint endPoint)
        {
            Logger.LogToConsole(LogType.Info, JsonConvert.SerializeObject(packet), "DEBUG-VO");
        }

        private void VoiceInbound(Core.Packets.Interfaces.IVoicePacket packet, System.Net.EndPoint endPoint)
        {
            Logger.LogToConsole(LogType.Info, JsonConvert.SerializeObject(packet), "DEBUG-VI");
        }

        private void MCCommInbound(Core.Packets.MCCommPacket packet)
        {
            Logger.LogToConsole(LogType.Info, JsonConvert.SerializeObject(packet), "DEBUG-MCI");
        }

        private void MCCommOutbound(Core.Packets.MCCommPacket packet)
        {
            Logger.LogToConsole(LogType.Info, JsonConvert.SerializeObject(packet), "DEBUG-MCO");
        }

        private void ExceptionError(Exception error)
        {
#if DEBUG
            Logger.LogToConsole(LogType.Warn, error.ToString(), "DEBUG_EXCEPTION");
#else
            Logger.LogToConsole(LogType.Warn, error.Message.ToString(), "DEBUG_EXCEPTION");
#endif
        }
#endregion

        #region Commands
        void HelpCommand(string[] args)
        {
            Logger.LogToConsole(LogType.Info, "Help - Shows a list of available commands.", "Commands");
            Logger.LogToConsole(LogType.Info, "Exit - Shuts down the server.", "Commands");
            Logger.LogToConsole(LogType.Info, "List - Lists the connected participants", "Commands");
            Logger.LogToConsole(LogType.Info, "Mute [key: ushort] - Mutes a participant.", "Commands");
            Logger.LogToConsole(LogType.Info, "Unmute [key: ushort] - Unmutes a participant.", "Commands");
            Logger.LogToConsole(LogType.Info, "Kick [key: ushort] - Kicks a participant.", "Commands");
            Logger.LogToConsole(LogType.Info, "Ban [key: ushort] - Bans a participant.", "Commands");
            Logger.LogToConsole(LogType.Info, "Unban [IPAddress: string] - Unbans an IP address.", "Commands");
            Logger.LogToConsole(LogType.Info, "Banlist - Shows a list of banned IP addresses.", "Commands");
            Logger.LogToConsole(LogType.Info, "SetProximity [Distance: int] - Sets the proximity distance.", "Commands");
            Logger.LogToConsole(LogType.Info, "ToggleProximity [Toggle: boolean] - Toggles proximity chat on or off", "Commands");
            Logger.LogToConsole(LogType.Info, "SetMotd [Message: string] - Sets the server MOTD.", "Commands");
            Logger.LogToConsole(LogType.Info, "ToggleEffects [Toggle: boolean] - Toggles the voice effect on or off.", "Commands");
            Logger.LogToConsole(LogType.Info, "Debug [Type: int] - Toggles individual debug logging on or off. 0 - SignallingInbound, 1 - SignallingOutbound, 2 - VoiceInbound, 3 - VoiceOutbound, 4 - MCCommInbound, 5 - MCCommOutbound", "Commands");
        }

        void ExitCommand(string[] args)
        {
            Logger.LogToConsole(LogType.Info, "Shutting down server...", "Server");
            server.Stop();
            server.Dispose();
            Environment.Exit(0);
        }

        void ListCommand(string[] args)
        {
            Logger.LogToConsole(LogType.Info, $"Connected participants: {server.Participants.Count}", "Commands");
            for (ushort i = 0; i < server.Participants.Count; i++)
            {
                var participant = server.Participants.ElementAt(i);
                if (participant.Value != null)
                    Logger.LogToConsole(LogType.Info, $"{i} - Binded: {participant.Value.Binded}, ServerMuted: {participant.Value.IsServerMuted}, Name: {participant.Value.Name ?? "N.A." }, Key: {participant.Key}", "Commands");
            }
        }

        void MuteCommand(string[] args)
        {
            if (args.Length != 1)
            {
                throw new ArgumentException("Usage: mute <key: ushort>");
            }

            if (ushort.TryParse(args[0], out ushort value))
            {
                if (server.Participants.TryGetValue(value, out var participant))
                {
                    participant.IsServerMuted = true;
                    Logger.LogToConsole(LogType.Success, $"Muted participant: {(string.IsNullOrWhiteSpace(participant.Name) ? value : participant.Name)}", "Commands");
                }
                else
                {
                    throw new Exception("Could not find participant!");
                }
            }
            else
            {
                throw new Exception("Invalid arguments!");
            }
        }

        void UnmuteCommand(string[] args)
        {
            if (args.Length != 1)
            {
                throw new ArgumentException("Usage: unmute <key: ushort>");
            }

            if (ushort.TryParse(args[0], out ushort value))
            {
                if (server.Participants.TryGetValue(value, out var participant))
                {
                    participant.IsMuted = false;
                    Logger.LogToConsole(LogType.Success, $"Unmuted participant: {(string.IsNullOrWhiteSpace(participant.Name) ? value : participant.Name)}", "Commands");
                }
                else
                {
                    throw new Exception("Could not find participant!");
                }
            }
            else
            {
                throw new Exception("Invalid arguments!");
            }
        }

        void KickCommand(string[] args)
        {
            if (args.Length != 1)
            {
                throw new ArgumentException("Usage: kick <key: ushort>");
            }

            if (ushort.TryParse(args[0], out ushort value))
            {
                if (server.Participants.TryGetValue(value, out var participant))
                {
                    server.RemoveParticipant(value, true, "Kicked").Wait();
                    participant.SignallingSocket.Shutdown(SocketShutdown.Both);
                    participant.SignallingSocket.Close();
                    Logger.LogToConsole(LogType.Success, $"Kicked participant: {(string.IsNullOrWhiteSpace(participant.Name) ? value : participant.Name)}", "Commands");
                }
                else
                {
                    throw new Exception("Could not find participant!");
                }
            }
            else
            {
                throw new Exception("Invalid arguments!");
            }
        }

        void BanCommand(string[] args)
        {
            if (args.Length != 1)
            {
                throw new ArgumentException("Usage: ban <key: ushort>");
            }

            if (ushort.TryParse(args[0], out ushort value))
            {
                if (server.Participants.TryGetValue(value, out var participant))
                {
                    var ip = participant.SignallingSocket.RemoteEndPoint?.ToString()?.Split(":").FirstOrDefault();
                    if (!string.IsNullOrWhiteSpace(ip))
                    {
                        server.Banlist.IPBans.Add(ip);
                        ServerData.SaveBanlist(server.Banlist);
                        server.RemoveParticipant(value, true, "Banned").Wait();
                        participant.SignallingSocket.Shutdown(SocketShutdown.Both);
                        participant.SignallingSocket.Close();
                        Logger.LogToConsole(LogType.Success, $"Banned participant: {(string.IsNullOrWhiteSpace(participant.Name) ? value : participant.Name)}", "Commands");
                    }
                }
                else
                {
                    throw new Exception("Could not find participant!");
                }
            }
            else
            {
                throw new Exception("Invalid arguments!");
            }
        }

        void UnbanCommand(string[] args)
        {
            if (args.Length != 1)
            {
                throw new ArgumentException("Usage: unban <ipaddress: string>");
            }

            if (server.Banlist.IPBans.Contains(args[0]))
            {
                server.Banlist.IPBans.Remove(args[0]);
                Logger.LogToConsole(LogType.Success, $"Unbanned IP address: {args[0]}", "Commands");
            }
            else
            {
                throw new Exception("Could not find IP Address!");
            }
        }

        void BanlistCommand(string[] args)
        {
            Logger.LogToConsole(LogType.Info, $"Banned IP Addresses: {server.Banlist.IPBans.Count}", "Commands");
            for (int i = 0; i < server.Banlist.IPBans.Count; i++)
            {
                Logger.LogToConsole(LogType.Info, server.Banlist.IPBans[i], "Commands");
            }
        }

        void SetProximityCommand(string[] args)
        {
            if (args.Length != 1)
            {
                throw new ArgumentException("Usage: setproximity <distance: int>");
            }

            if (ushort.TryParse(args[0], out ushort value))
            {
                if (value > 120 || value < 1)
                    throw new ArgumentException("Invalid distance! Distance can only be between 1 and 120!");
                server.ServerProperties.ProximityDistance = value;
                Logger.LogToConsole(LogType.Success, $"Set proximity distance: {value}", "Commands");
            }
            else
            {
                throw new Exception("Invalid arguments!");
            }
        }

        void ToggleProximityCommand(string[] args)
        {
            if (args.Length != 1)
            {
                throw new ArgumentException("Usage: toggleproximity <toggle: boolean>");
            }

            if (bool.TryParse(args[0], out bool value))
            {
                server.ServerProperties.ProximityToggle = value;
                Logger.LogToConsole(LogType.Success, $"Set proximity toggle: {value}", "Commands");
            }
            else
            {
                throw new Exception("Invalid arguments!");
            }
        }

        void SetMotdCommand(string[] args)
        {
            if (args.Length != 1)
            {
                throw new ArgumentException("Usage: setmotd <message: string>");
            }

            if (!string.IsNullOrWhiteSpace(args[0]))
            {
                server.ServerProperties.ServerMOTD = args[0];
                Logger.LogToConsole(LogType.Success, $"Set MOTD: {args[0]}", "Commands");
            }
            else
            {
                throw new Exception("Argument cannot be null whitespace!");
            }
        }

        void ToggleEffectsCommand(string[] args)
        {
            if (args.Length != 1)
            {
                throw new ArgumentException("Usage: toggleeffects <toggle: boolean>");
            }

            if (bool.TryParse(args[0], out bool value))
            {
                server.ServerProperties.VoiceEffects = value;
                Logger.LogToConsole(LogType.Success, $"Set effects toggle: {value}", "Commands");
            }
            else
            {
                throw new Exception("Invalid arguments!");
            }
        }

        void DebugCommand(string[] args)
        {
            if (args.Length != 1)
            {
                throw new ArgumentException("Usage: debug <type: int>");
            }

            if (ushort.TryParse(args[0], out ushort value))
            {
                switch(value)
                {
                    case 0:
                        server.Signalling.LogInbound = !server.Signalling.LogInbound;
                        Logger.LogToConsole(LogType.Success, $"Set signalling inbound debug: {server.Signalling.LogInbound}", "Commands");
                        break;
                    case 1:
                        server.Signalling.LogOutbound = !server.Signalling.LogOutbound;
                        Logger.LogToConsole(LogType.Success, $"Set signalling outbound debug: {server.Signalling.LogOutbound}", "Commands");
                        break;
                    case 2:
                        server.Voice.LogInbound = !server.Voice.LogInbound;
                        Logger.LogToConsole(LogType.Success, $"Set voice inbound debug: {server.Voice.LogInbound}", "Commands");
                        break;
                    case 3:
                        server.Voice.LogOutbound = !server.Voice.LogOutbound;
                        Logger.LogToConsole(LogType.Success, $"Set voice outbound debug: {server.Voice.LogOutbound}", "Commands");
                        break;
                    case 4:
                        server.MCComm.LogInbound = !server.MCComm.LogInbound;
                        Logger.LogToConsole(LogType.Success, $"Set mccomm inbound debug: {server.MCComm.LogInbound}", "Commands");
                        break;
                    case 5:
                        server.MCComm.LogOutbound = !server.MCComm.LogOutbound;
                        Logger.LogToConsole(LogType.Success, $"Set mccomm outbound debug: {server.MCComm.LogOutbound}", "Commands");
                        break;
                    default:
                        throw new Exception("Invalid type specified!");
                }
            }
            else
            {
                throw new Exception("Invalid arguments!");
            }
        }
    }
    #endregion
}