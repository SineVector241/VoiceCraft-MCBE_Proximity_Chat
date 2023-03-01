using System;
using System.Threading;
using System.Threading.Tasks;
using VoiceCraft_Server.Servers;

namespace VoiceCraft_Server
{
    public class MainEntry
    {
        public const string Version = "v1.3.0-alpha";
        public MainEntry()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"VoiceCraft - Version: {Version}");
            Console.ResetColor();

            Console.Title = $"VoiceCraft - {Version}: Starting...";
        }

        public async Task StartServer()
        {
            if (ServerProperties._serverProperties.SignallingPort_UDP == ServerProperties._serverProperties.VoicePort_UDP)
            {
                Logger.LogToConsole(LogType.Error, "Signalling Port cannot be the same as the Voice Port. Closing in 5 seconds...", nameof(MainEntry));
                Console.Title = $"VoiceCraft - {Version}: ERROR";
                await Task.Delay(5000);
                return;
            }

            new Thread(async () => { await new Voice().StartServer(); }) { IsBackground = true }.Start();
            //new Thread(() => { new TCP(); }) { IsBackground = true }.Start();
            new Thread(async () => { await new Signalling().StartServer(); }) { IsBackground = true }.Start();

            Console.Title = $"VoiceCraft - {Version}: Running";
            Logger.LogToConsole(LogType.Success, "Server Started: Type EXIT to shutdown the server...", nameof(MainEntry));

            ServerMetadata.timer = new Timer(new TimerCallback(ServerMetadata.CheckParticipants), null, 5000, 5000);

            while (true)
            {
                var cmd = Console.ReadLine();
                if (cmd.ToLower() == "exit")
                {
                    break;
                }
            }
        }
    }
}
