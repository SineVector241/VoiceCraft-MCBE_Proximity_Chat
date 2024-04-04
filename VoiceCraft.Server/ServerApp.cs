using VoiceCraft.Server.Data;

namespace VoiceCraft.Server
{
    public class ServerApp
    {
        VoiceCraftServer Server { get; set; }
        Properties ServerProperties { get; set; } = new Properties();

        public ServerApp()
        {
            Server = new VoiceCraftServer(ServerProperties);

            Server.OnFailed += OnFailed;
            Server.VoiceCraftSocket.OnStarted += VoiceCraftOnStarted;
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
        private void OnFailed(Exception ex)
        {
            Logger.LogToConsole(LogType.Error, $"Server Failed - Reason: {ex.Message}", nameof(ServerApp));
        }

        private void VoiceCraftOnStarted()
        {
            Logger.LogToConsole(LogType.Success, "VoiceCraft Server Started", nameof(VoiceCraft));
        }

        private void ParticipantJoined(VoiceCraftParticipant participant)
        {
            Logger.LogToConsole(LogType.Success, "Participant Connected!", nameof(VoiceCraft));
        }

        private void ParticipantLeft(VoiceCraftParticipant participant, string? reason = null)
        {
            Logger.LogToConsole(LogType.Warn, $"Participant Disconnected - Reason: {reason}", nameof(VoiceCraft));
        }
        #endregion
    }
}