using System;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using VCSignalling_Packet;
using VoiceCraft_Server.Servers;

namespace VoiceCraft_Server
{
    public class MainEntry
    {
        public const string Version = "v1.3.0-alpha";
        private bool failed = false;
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

            var voice = new Voice();
            var mcComm = new MCComm();
            var signalling = new Signalling();

            voice.StartServer();
            signalling.StartServer();

            voice.OnFail += OnFail;
            mcComm.OnFail += OnFail;
            signalling.OnFail += OnFail;

            Console.Title = $"VoiceCraft - {Version}: Running";
            Logger.LogToConsole(LogType.Success, "Server Started: Type EXIT to shutdown the server...", nameof(MainEntry));

            ServerMetadata.timer = new Timer(new TimerCallback(ServerMetadata.CheckParticipants), null, 5000, 5000);

            while (true)
            {
                var cmd = Console.ReadLine();
                if(cmd.ToLower() == "test")
                {
                    for (int i = 0; i < ServerMetadata.voiceParticipants.Count; i++)
                    {
                        await signalling.serverSocket.SendToAsync(new ArraySegment<byte>(new SignallingPacket() { PacketDataIdentifier = PacketIdentifier.Login, PacketLoginId = "aaaaa", PacketName = "test" }.GetPacketDataStream()), SocketFlags.None, ServerMetadata.voiceParticipants[i].SignallingAddress);
                    }
                }
                if (cmd.ToLower() == "exit")
                {
                    break;
                }
            }
        }

        private async void OnFail(string reason)
        {
            Console.Title = $"VoiceCraft - {Version}: Failed";
            Logger.LogToConsole(LogType.Error, "Failed to start server. Closing server in 10 seconds...", nameof(MainEntry));
            await Task.Delay(10000);
            Environment.Exit(0);
        }
    }
}
