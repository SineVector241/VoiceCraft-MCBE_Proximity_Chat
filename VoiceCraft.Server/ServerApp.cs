using Newtonsoft.Json;
using VoiceCraft.Network.Sockets;
using VoiceCraft.Server.Data;

namespace VoiceCraft.Server
{
    public class ServerApp
    {
        VoiceCraftServer Server { get; set; }

        public ServerApp()
        {
            Console.Title = $"VoiceCraft - {VoiceCraftServer.Version}: Starting...";
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
            Server.VoiceCraftSocket.OnExceptionError += ExceptionError; ;
            Server.MCComm.OnInboundPacket += MCCommInbound;
            Server.MCComm.OnOutboundPacket += MCCommOutbound;
            Server.MCComm.OnExceptionError += ExceptionError;
        }

        public void Start()
        {
            Server.Start();

            Console.ReadLine();
        }

        #region Server Event Methods
        private void ServerStarted()
        {
            Logger.LogToConsole(LogType.Success, "Server Started", nameof(VoiceCraftServer));
        }

        private void ServerSocketStarted(Type socket)
        {
            Logger.LogToConsole(LogType.Success, $"{socket.Name} Socket Started", socket.Name);
        }

        private void ServerFailed(Exception ex)
        {
            Logger.LogToConsole(LogType.Error, $"Server Failed - Reason: {ex.Message}, Exception Type: {ex.GetType().Name}", nameof(VoiceCraftServer));
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
    }
}