using System;
using System.Linq;
using System.Threading.Tasks;
using VoiceCraft_Server.Data;
using VoiceCraft_Server.Servers;

namespace VoiceCraft_Server
{
    public class MainEntry
    {
        public const string Version = "v1.3.0-alpha";
        public readonly ServerData serverData;

        private Signalling signalServer;
        private Voice voiceServer;
        public MainEntry()
        {
            serverData = new ServerData();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"VoiceCraft - Version: {Version}");
            Console.ResetColor();

            Console.Title = $"VoiceCraft - {Version}: Starting...";
            serverData.Start();
        }

        public async Task StartServer()
        {
            ServerEvents.OnStarted += OnStartedService;
            ServerEvents.OnFailed += OnFailedService;

            //Idk why I did it this way
            new ServerProperties();

            while (true)
            {
                var cmd = Console.ReadLine();
                if(cmd.ToLower() == "test")
                {
                    var p = serverData.GetParticipants().FirstOrDefault();
                    if(p != null)
                    {
                        Console.WriteLine(p.MinecraftData.Position);
                    }
                }
                if (cmd.ToLower() == "exit")
                {
                    break;
                }
            }
        }

        private async Task OnFailedService(string service, string reason)
        {
            Console.Title = $"VoiceCraft - {Version}: Failed";
            Logger.LogToConsole(LogType.Error, "Failed to start server. Closing server in 10 seconds...", nameof(MainEntry));
            await Task.Delay(10000);
            Environment.Exit(0);
        }

        private Task OnStartedService(string service)
        {
            switch(service)
            {
                case nameof(ServerProperties):
                    new MCComm(serverData);
                    break;
                case nameof(MCComm):
                    signalServer = new Signalling(serverData);
                    signalServer.Start();
                    break;
                case nameof(Signalling):
                    voiceServer = new Voice(serverData);
                    voiceServer.Start();
                    break;
                case nameof(Voice):
                    Console.Title = $"VoiceCraft - {Version}: Running";
                    Logger.LogToConsole(LogType.Success, "Server Started: Type EXIT to shutdown the server...", nameof(MainEntry));
                    break;
            }
            return Task.CompletedTask;
        }
    }
}
