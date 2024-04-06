using VoiceCraft.Network.Sockets;
using VoiceCraft.Server.Data;

namespace VoiceCraft.Server
{
    public class ServerApp
    {
        VoiceCraftServer Server { get; set; }
        Properties ServerProperties { get; set; } = new Properties();

        public ServerApp()
        {
            Server = new VoiceCraftServer(ServerProperties, new List<string>());

            Server.OnStopped += OnStopped;
            Server.VoiceCraftSocket.OnStarted += VoiceCraftOnStarted;
            Server.MCComm.OnStarted += MCCommOnStarted;
            Server.MCComm.OnServerConnected += MCCommServerConnected;
            Server.MCComm.OnServerDisconnected += MCCommServerDisconnected;
            Server.OnParticipantJoined += ParticipantJoined;
            Server.OnParticipantLeft += ParticipantLeft;
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
            Console.WriteLine($"[v1.0.3][{VoiceCraftServer.Version}]=========================[DEBUG]\n");
#else
            Console.WriteLine($"[v1.0.3][{VoiceCraftServer.Version}]=======================[RELEASE]\n");
#endif

            Server.Start();

            Console.ReadLine();
        }

        #region Server Event Methods
        private void OnStopped(string? reason = null)
        {
            Logger.LogToConsole(LogType.Error, $"Server Stopped - Reason: {reason}", nameof(ServerApp));
        }

        private void VoiceCraftOnStarted()
        {
            Logger.LogToConsole(LogType.Success, "VoiceCraft Server Started", nameof(VoiceCraft));
        }

        private void MCCommOnStarted()
        {
            Logger.LogToConsole(LogType.Success, $"MCComm Server Started - LoginKey: {Server.MCComm.LoginKey}", nameof(MCComm));
        }

        private void MCCommServerConnected(string token, string address)
        {
            Logger.LogToConsole(LogType.Success, $"MCComm Server Connected - Token: {token}, Address: {address}", nameof(MCComm));
        }

        private void MCCommServerDisconnected(int timeoutDiff, string token)
        {
            Logger.LogToConsole(LogType.Warn, $"MCComm Server Disconnected - Token: {token}, Timeout: {timeoutDiff}", nameof(MCComm));
        }

        private void ParticipantJoined(VoiceCraftParticipant participant)
        {
            Logger.LogToConsole(LogType.Success, $"Participant Connected - Key: {participant.Key}", nameof(VoiceCraft));
        }

        private void ParticipantLeft(VoiceCraftParticipant participant, string? reason = null)
        {
            Logger.LogToConsole(LogType.Warn, $"Participant Disconnected - Key: {participant.Key}, Reason: {reason}", nameof(VoiceCraft));
        }
        #endregion
    }
}