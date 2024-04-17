using Newtonsoft.Json;
using System.Net;
using VoiceCraft.Network.Sockets;
using VoiceCraft.Server.Data;

namespace VoiceCraft.Server
{
    public class ServerApp
    {
        VoiceCraftServer Server { get; set; }

        public ServerApp()
        {
            Console.Title = $"VoiceCraft - {VoiceCraftServer.Version}: Loading...";
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(@"__     __    _           ____            __ _");
            Console.WriteLine(@"\ \   / /__ (_) ___ ___ / ___|_ __ __ _ / _| |_");
            Console.WriteLine(@" \ \ / / _ \| |/ __/ _ \ |   | '__/ _` | |_| __|");
            Console.WriteLine(@"  \ V / (_) | | (_|  __/ |___| | | (_| |  _| |_");
            Console.WriteLine(@"   \_/ \___/|_|\___\___|\____|_|  \__,_|_|  \__|");
#if DEBUG
            Console.WriteLine($"[v1.0.3][{VoiceCraftServer.Version}]=========================[DEBUG]\n");
#else
            Console.WriteLine($"[v1.0.3][{VoiceCraftServer.Version}]=======================[RELEASE]\n");
#endif

            var properties = Properties.LoadProperties();
            var banlist = Properties.LoadBanlist();
            Server = new VoiceCraftServer(properties, banlist);

            foreach (var channel in Server.ServerProperties.Channels)
            {
                Logger.LogToConsole(LogType.Success, $"Channel Added - Name: {channel.Name}, Password?: {!string.IsNullOrWhiteSpace(channel.Password)}", "Channel");
            }

            //Server Events
            Server.OnStarted += ServerStarted;
            Server.OnSocketStarted += ServerSocketStarted;
            Server.OnFailed += ServerFailed;
            Server.OnStopped += OnStopped;
            Server.OnParticipantJoined += ParticipantJoined;
            Server.OnParticipantLeft += ParticipantLeft;

            //MCComm Socket
            Server.MCComm.OnServerConnected += MCCommServerConnected;
            Server.MCComm.OnServerDisconnected += MCCommServerDisconnected;

            //Debug Stuff
            Server.VoiceCraftSocket.OnInboundPacket += VoiceCraftSocketInbound;
            Server.VoiceCraftSocket.OnOutboundPacket += VoiceCraftSocketOutbound;
            Server.VoiceCraftSocket.OnExceptionError += ExceptionError;
            Server.MCComm.OnInboundPacket += MCCommInbound;
            Server.MCComm.OnOutboundPacket += MCCommOutbound;
            Server.MCComm.OnExceptionError += ExceptionError;

            //Command Register
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
        }

        public void Start()
        {
            Console.Title = $"VoiceCraft - {VoiceCraftServer.Version}: Starting...";
            Server.Start();

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

        #region Server Event Methods
        private void ServerStarted()
        {
            Console.Title = $"VoiceCraft - {VoiceCraftServer.Version}: Started.";

            if(Server.ServerProperties.ConnectionType == ConnectionTypes.Server || Server.ServerProperties.ConnectionType == ConnectionTypes.Hybrid)
                Logger.LogToConsole(LogType.Success, $"Server Started - Key: {Server.ServerProperties.PermanentServerKey}", nameof(VoiceCraftServer));
            else
                Logger.LogToConsole(LogType.Success, $"Server Started!", nameof(VoiceCraftServer));
        }

        private void ServerSocketStarted(Type socket)
        {
            Logger.LogToConsole(LogType.Success, $"{socket.Name} Socket Started", socket.Name);
        }

        private void ServerFailed(Exception ex)
        {
            Console.Title = $"VoiceCraft - {VoiceCraftServer.Version}: Failed.";
            Logger.LogToConsole(LogType.Error, $"Server Failed - Reason: {ex.Message}, Exception Type: {ex.GetType().Name}", nameof(VoiceCraftServer));
            Logger.LogToConsole(LogType.Error, "Shutting down server in 10 seconds...", nameof(VoiceCraftServer));
            Task.Delay(10000).Wait();
            ExitCommand([]);
        }

        private void OnStopped(string? reason = null)
        {
            Logger.LogToConsole(LogType.Warn, $"Server Stopped - Reason: {reason}", nameof(VoiceCraftServer));
        }

        private void ParticipantJoined(VoiceCraftParticipant participant)
        {
            Logger.LogToConsole(LogType.Success, $"Participant Connected - Key: {participant.Key}", nameof(VoiceCraftServer));
        }

        private void ParticipantLeft(VoiceCraftParticipant participant, string? reason = null)
        {
            Logger.LogToConsole(LogType.Warn, $"Participant Disconnected - Key: {participant.Key}, Reason: {reason}", nameof(VoiceCraftServer));
        }

        private void MCCommInbound(Core.Packets.MCCommPacket packet)
        {
            Logger.LogToConsole(LogType.Info, JsonConvert.SerializeObject(packet), "DEBUG-MI");
        }

        private void MCCommOutbound(Core.Packets.MCCommPacket packet)
        {
            Logger.LogToConsole(LogType.Info, JsonConvert.SerializeObject(packet), "DEBUG-MO");
        }
        #endregion

        #region MCComm Server Events
        private void MCCommServerConnected(string token, string address)
        {
            Logger.LogToConsole(LogType.Success, $"MCComm Server Connected - Token: {token}, Address: {address}", nameof(MCComm));
        }

        private void MCCommServerDisconnected(int timeoutDiff, string token)
        {
            Logger.LogToConsole(LogType.Warn, $"MCComm Server Disconnected - Token: {token}, Timeout: {timeoutDiff}", nameof(MCComm));
        }

        private void VoiceCraftSocketInbound(Core.Packets.VoiceCraftPacket packet, Network.NetPeer peer)
        {
            Logger.LogToConsole(LogType.Info, JsonConvert.SerializeObject(packet), "DEBUG-VI");
        }

        private void VoiceCraftSocketOutbound(Core.Packets.VoiceCraftPacket packet, Network.NetPeer peer)
        {
            Logger.LogToConsole(LogType.Info, JsonConvert.SerializeObject(packet), "DEBUG-VO");
        }
        #endregion

        private void ExceptionError(Exception error)
        {
#if DEBUG
            Logger.LogToConsole(LogType.Warn, error.ToString(), "DEBUG_EXCEPTION");
#else
            Logger.LogToConsole(LogType.Warn, error.Message.ToString(), "DEBUG_EXCEPTION");
#endif
        }

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
            Logger.LogToConsole(LogType.Info, "Debug [Type: int] - Toggles individual debug logging on or off. 0 - VoiceCraftInbound, 1 - VoiceCraftOutbound, 2 - MCCommInbound, 3 - MCCommOutbound", "Commands");
        }

        void ExitCommand(string[] args)
        {
            Console.Title = $"VoiceCraft - {VoiceCraftServer.Version}: Shutting Down...";
            Logger.LogToConsole(LogType.Info, "Shutting down server...", "Server");
            Server.Stop("Shutdown.");
            Server.Dispose();
            Environment.Exit(0);
        }

        void ListCommand(string[] args)
        {
            Logger.LogToConsole(LogType.Info, $"Connected participants: {Server.Participants.Count}", "Commands");
            for (ushort i = 0; i < Server.Participants.Count; i++)
            {
                var participant = Server.Participants.ElementAt(i);
                if (participant.Value != null)
                    Logger.LogToConsole(LogType.Info, $"{i} - Binded: {participant.Value.Binded}, ServerMuted: {participant.Value.ServerMuted}, Name: {participant.Value.Name ?? "N.A."}, Key: {participant.Value.Key}, EnvironmentId: {participant.Value.EnvironmentId}", "Commands");
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
                var participant = Server.Participants.FirstOrDefault(x => x.Value.Key == value);
                if (participant.Value != null)
                {
                    participant.Value.ServerMuted = true;
                    Logger.LogToConsole(LogType.Success, $"Muted participant: {(string.IsNullOrWhiteSpace(participant.Value.Name) ? value : participant.Value.Name)}", "Commands");
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
                var participant = Server.Participants.FirstOrDefault(x => x.Value.Key == value);
                if (participant.Value != null)
                {
                    participant.Value.Muted = false;
                    Logger.LogToConsole(LogType.Success, $"Unmuted participant: {(string.IsNullOrWhiteSpace(participant.Value.Name) ? value : participant.Value.Name)}", "Commands");
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
                var participant = Server.Participants.FirstOrDefault(x => x.Value.Key == value);
                if (participant.Value != null)
                {
                    participant.Key.Disconnect("Server Kick", true);
                    Logger.LogToConsole(LogType.Success, $"Kicked participant: {(string.IsNullOrWhiteSpace(participant.Value.Name) ? value : participant.Value.Name)}", "Commands");
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
                var participant = Server.Participants.FirstOrDefault(x => x.Value.Key == value);
                if (participant.Value != null)
                {
                    Server.Banlist.Add(((IPEndPoint)participant.Key.RemoteEndPoint).Address.ToString());
                    Properties.SaveBanlist(Server.Banlist);
                    participant.Key.Disconnect("Server Banned", true);
                    Logger.LogToConsole(LogType.Success, $"Banned participant: {(string.IsNullOrWhiteSpace(participant.Value.Name) ? value : participant.Value.Name)}", "Commands");
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

            if (Server.Banlist.Contains(args[0]))
            {
                Server.Banlist.Remove(args[0]);
                Properties.SaveBanlist(Server.Banlist);
                Logger.LogToConsole(LogType.Success, $"Unbanned IP address: {args[0]}", "Commands");
            }
            else
            {
                throw new Exception("Could not find IP Address!");
            }
        }

        void BanlistCommand(string[] args)
        {
            Logger.LogToConsole(LogType.Info, $"Banned IP Addresses: {Server.Banlist.Count}", "Commands");
            for (int i = 0; i < Server.Banlist.Count; i++)
            {
                Logger.LogToConsole(LogType.Info, Server.Banlist[i], "Commands");
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
                Server.ServerProperties.ProximityDistance = value;
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
                Server.ServerProperties.ProximityToggle = value;
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
                Server.ServerProperties.ServerMOTD = args[0];
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
                Server.ServerProperties.VoiceEffects = value;
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
                switch (value)
                {
                    case 0:
                        Server.VoiceCraftSocket.LogInbound = !Server.VoiceCraftSocket.LogInbound;
                        Logger.LogToConsole(LogType.Success, $"Set voicecraft inbound debug: {Server.VoiceCraftSocket.LogInbound}", "Commands");
                        break;
                    case 1:
                        Server.VoiceCraftSocket.LogOutbound = !Server.VoiceCraftSocket.LogOutbound;
                        Logger.LogToConsole(LogType.Success, $"Set voicecraft outbound debug: {Server.VoiceCraftSocket.LogOutbound}", "Commands");
                        break;
                    case 2:
                        Server.MCComm.LogInbound = !Server.MCComm.LogInbound;
                        Logger.LogToConsole(LogType.Success, $"Set mccomm inbound debug: {Server.MCComm.LogInbound}", "Commands");
                        break;
                    case 3:
                        Server.MCComm.LogOutbound = !Server.MCComm.LogOutbound;
                        Logger.LogToConsole(LogType.Success, $"Set mccomm outbound debug: {Server.MCComm.LogOutbound}", "Commands");
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