using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using VoiceCraft_Server.Data;
using VoiceCraft_Server.Servers;

namespace VoiceCraft_Server
{
    public class MainEntry
    {
        public const string Version = "v1.3.5-alpha";
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
                var input = Console.ReadLine();
                if (input.ToLower() == "exit")
                {
                    break;
                }

                var splitCmd = input.Split(' ');
                var cmd = splitCmd[0].ToLower();

                try
                {
                    switch (cmd)
                    {
                        case "list":
                            var p1 = serverData.GetParticipants();
                            Logger.LogToConsole(LogType.Info, $"Connected Participants {p1.Count}", nameof(MainEntry));
                            //Thread safety
                            Parallel.ForEach(p1, participant => {
                                Logger.LogToConsole(LogType.Info, $"Key: {participant.LoginKey}, Binded: {participant.Binded}, IsMuted: {participant.Muted}, Name: {participant.MinecraftData.Gamertag}, Dimension: {participant.MinecraftData.DimensionId}, Position: {participant.MinecraftData.Position}, Rotation: {participant.MinecraftData.Rotation}", nameof(MainEntry));
                            });
                            break;
                        case "mute":
                            var muteKeyArg = splitCmd.ElementAt(1);
                            if (string.IsNullOrEmpty(muteKeyArg))
                            {
                                Logger.LogToConsole(LogType.Error, "Error. Key argument cannot be empty!", nameof(MainEntry));
                                break;
                            }

                            var p2 = serverData.GetParticipants();
                            var part1 = p2.AsParallel().FirstOrDefault(x => x.LoginKey == muteKeyArg);
                            if(part1 == null)
                            {
                                Logger.LogToConsole(LogType.Error, "Error. Participant could not be found!", nameof(MainEntry));
                                break;
                            }

                            part1.Muted = true;
                            Logger.LogToConsole(LogType.Success, "Successfully muted participant.", nameof(MainEntry));
                            break;
                        case "unmute":
                            var unmuteKeyArg = splitCmd.ElementAt(1);
                            if (string.IsNullOrEmpty(unmuteKeyArg))
                            {
                                Logger.LogToConsole(LogType.Error, "Error. Key argument cannot be empty!", nameof(MainEntry));
                                break;
                            }

                            var p3 = serverData.GetParticipants();
                            var part2 = p3.AsParallel().FirstOrDefault(x => x.LoginKey == unmuteKeyArg);
                            if (part2 == null)
                            {
                                Logger.LogToConsole(LogType.Error, "Error. Participant could not be found!", nameof(MainEntry));
                                break;
                            }

                            part2.Muted = false;
                            Logger.LogToConsole(LogType.Success, "Successfully unmuted participant.", nameof(MainEntry));
                            break;
                        case "kick":
                            var kickKeyArg = splitCmd.ElementAt(1);
                            if (string.IsNullOrEmpty(kickKeyArg))
                            {
                                Logger.LogToConsole(LogType.Error, "Error. Key argument cannot be empty!", nameof(MainEntry));
                                break;
                            }

                            var p4 = serverData.GetParticipants();
                            var part3 = p4.AsParallel().FirstOrDefault(x => x.LoginKey == kickKeyArg);
                            if (part3 == null)
                            {
                                Logger.LogToConsole(LogType.Error, "Error. Participant could not be found!", nameof(MainEntry));
                                break;
                            }

                            serverData.RemoveParticipant(part3, true);
                            Logger.LogToConsole(LogType.Success, "Successfully kicked participant.", nameof(MainEntry));
                            break;
                        case "ban":
                            var banKeyArg = splitCmd.ElementAt(1);
                            if (string.IsNullOrEmpty(banKeyArg))
                            {
                                Logger.LogToConsole(LogType.Error, "Error. Key argument cannot be empty!", nameof(MainEntry));
                                break;
                            }

                            var p5 = serverData.GetParticipants();
                            var part4 = p5.AsParallel().FirstOrDefault(x => x.LoginKey == banKeyArg);
                            if (part4 == null)
                            {
                                Logger.LogToConsole(LogType.Error, "Error. Participant could not be found!", nameof(MainEntry));
                                break;
                            }
                            ServerProperties.BanIp(part4.SocketData.SignallingAddress.ToString().Split(':').FirstOrDefault());
                            serverData.RemoveParticipant(part4, true);
                            Logger.LogToConsole(LogType.Success, "Successfully banned participant.", nameof(MainEntry));
                            break;
                        case "unban":
                            var unbanIpArg = splitCmd.ElementAt(1);
                            if (string.IsNullOrEmpty(unbanIpArg))
                            {
                                Logger.LogToConsole(LogType.Error, "Error. ipAddress argument cannot be empty!", nameof(MainEntry));
                                break;
                            }

                            ServerProperties.UnbanIp(unbanIpArg);
                            break;
                        case "banlist":
                            Logger.LogToConsole(LogType.Info, $"Banned IP's: {ServerProperties._banlist.BannedIPs.Count}", nameof(MainEntry));
                            foreach(var ip in ServerProperties._banlist.BannedIPs)
                            {
                                Logger.LogToConsole(LogType.Info, ip, nameof(MainEntry));
                            }
                            break;
                        case "setproximity":
                            var proxArg = splitCmd.ElementAt(1);
                            if (string.IsNullOrEmpty(proxArg))
                            {
                                Logger.LogToConsole(LogType.Error, "Error. Distance argument cannot be empty!", nameof(MainEntry));
                                break;
                            }

                            var proxArgInt = 0;
                            int.TryParse(proxArg, out proxArgInt);

                            if (proxArgInt <= 0)
                            {
                                Logger.LogToConsole(LogType.Error, "Error. Distance argument must be higher than 0!", nameof(MainEntry));
                                break;
                            }

                            ServerProperties._serverProperties.ProximityDistance = proxArgInt;
                            Logger.LogToConsole(LogType.Success, $"Successfully set proximity distance to {proxArgInt}", nameof(MainEntry));
                            break;
                        case "help":
                            Logger.LogToConsole(LogType.Info, "exit: Shuts down the server.", nameof(MainEntry));
                            Logger.LogToConsole(LogType.Info, "mute [key: string]: Mutes a participant.", nameof(MainEntry));
                            Logger.LogToConsole(LogType.Info, "unmute [key: string]: Unmutes a participant.", nameof(MainEntry));
                            Logger.LogToConsole(LogType.Info, "kick [key: string]: Kicks a participant.", nameof(MainEntry));
                            Logger.LogToConsole(LogType.Info, "ban [key: string]: Bans a participant. Doesn't blacklist the key but blacklists the participant's IP address", nameof(MainEntry));
                            Logger.LogToConsole(LogType.Info, "unban [ipAddress: string]: Unbans an IPAddress", nameof(MainEntry));
                            Logger.LogToConsole(LogType.Info, "banlist: Shows the ip addresses that are banned.", nameof(MainEntry));
                            Logger.LogToConsole(LogType.Info, "setproximity [distance: int]: Sets the proximity distance (Defaults to serverProperties.json setting on server restart).", nameof(MainEntry));
                            break;
                        default:
                            Logger.LogToConsole(LogType.Error, $"Could not find command that matches {cmd.ToLower()}", nameof(MainEntry));
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogToConsole(LogType.Error, ex.Message, nameof(MainEntry));
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
            switch (service)
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
                    Logger.LogToConsole(LogType.Success, "Server Started: Type HELP to view available commands...", nameof(MainEntry));
                    break;
            }
            return Task.CompletedTask;
        }
    }
}
