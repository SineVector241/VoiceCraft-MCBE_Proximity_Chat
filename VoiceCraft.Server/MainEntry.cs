using VoiceCraft.Core.Server;

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
            Console.WriteLine("[v1.0.0]========================================\n");
            try
            {
                server.ServerProperties = serverData.LoadProperties();
                server.Banlist = serverData.LoadBanlist();
                server.Start();

                Console.ReadLine();
            }
            catch(Exception ex)
            {
#if DEBUG
                Logger.LogToConsole(LogType.Error, ex.ToString(), "Server");
#else
            Logger.LogToConsole(LogType.Error, exception.Message.ToString(), "Socket");
#endif
                Logger.LogToConsole(LogType.Info, "Shutting down server in 10 seconds...", "Server");
                await Task.Delay(10000);
            }
        }

        private void ServerError(Exception ex)
        {
#if DEBUG
            Logger.LogToConsole(LogType.Error, ex.ToString(), "Socket");
#else
            Logger.LogToConsole(LogType.Error, exception.Message.ToString(), "Socket");
#endif
        }

        private void SignallingStarted()
        {
            Logger.LogToConsole(LogType.Success, $"Signalling Started - Port:{server.ServerProperties.SignallingPortTCP} TCP", "Socket");
        }

        private void VoiceStarted()
        {
            Logger.LogToConsole(LogType.Success, $"Voice Started      - Port:{server.ServerProperties.VoicePortUDP} UDP", "Socket");
            if (server.ServerProperties.ConnectionType == ConnectionTypes.Client)
            {
                Logger.LogToConsole(LogType.Success, "Server Started!", "Server");
                Console.Title = $"VoiceCraft - {VoiceCraftServer.Version}: Running.";
            }
        }

        private void WebserverStarted()
        {
            Logger.LogToConsole(LogType.Success, $"MCComm Started     - Port:{server.ServerProperties.MCCommPortTCP} TCP", "Socket");
            Logger.LogToConsole(LogType.Success, "Server Started!", "Server");
            Logger.LogToConsole(LogType.Success, $"MCComm Server Key:{server.ServerProperties.PermanentServerKey}", "Server");

            Console.Title = $"VoiceCraft - {VoiceCraftServer.Version}: Running.";
        }

        private void ParticipantConnected(VoiceCraftParticipant participant, ushort key)
        {
            Logger.LogToConsole(LogType.Success, $"Participant Connected: Key - {key}, Positioning Type - {participant.PositioningType}", "Server");
        }

        private void ParticipantBinded(VoiceCraftParticipant participant, ushort key)
        {
            Logger.LogToConsole(LogType.Success, $"Participant Binded: Key - {key}, Name - {participant.Name}", "Server");
        }

        private void ParticipantUnbinded(VoiceCraftParticipant participant, ushort key)
        {
            Logger.LogToConsole(LogType.Warn, $"Participant Unbinded: Key - {key}, Name - {participant.Name}", "Server");
        }

        private void ParticipantDisconnected(string reason, VoiceCraftParticipant participant, ushort key)
        {
            Logger.LogToConsole(LogType.Warn, $"Participant Disconnected: Key - {key}, Reason - {reason}", "Server");
        }
    }
}
