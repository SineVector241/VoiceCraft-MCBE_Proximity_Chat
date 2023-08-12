using VoiceCraft.Server.Helpers;
using VoiceCraft.Server.Sockets;

namespace VoiceCraft.Server
{
    public class MainEntry
    {
        readonly CancellationTokenSource CTS = new CancellationTokenSource();
        public const string Version = "v1.4.1-alpha";

        public MainEntry()
        {
            ServerEvents.OnStarted += ServiceStarted;
            ServerEvents.OnFailed += ServiceFailed;
        }

        private async Task ServiceFailed(string service, string reason)
        {
            Logger.LogToConsole(LogType.Error, "Failed to start server. Closing window in 10 seconds.", nameof(MainEntry));
            Console.Title = $"VoiceCraft - {Version}: Failed...";
            ServerEvents.InvokeStopping();
            await Task.Delay(10000);
            Environment.Exit(0);
        }

        private Task ServiceStarted(string service)
        {
            switch(service)
            {
                case nameof(MCComm):
                    _ = Task.Run(async () => {
                        await new Signalling().Start();
                    });
                    break;
                case nameof(Signalling):
                    _ = Task.Run(async () => {
                        await new Voice().Start();
                    });
                    break;
                case nameof(Voice):
                    Logger.LogToConsole(LogType.Success, "Server successfully started!", nameof(MainEntry));
                    Console.Title = $"VoiceCraft - {Version}: Started.";
                    break;
            }

            return Task.CompletedTask;
        }

        public async Task Start()
        {
            ServerProperties.LoadProperties();
            ServerData.StartTimer(CTS.Token);

            if (ServerProperties.Properties.ConnectionType != ConnectionTypes.Client)
            {
                _ = Task.Run(() =>
                {
                    new MCComm().Start();
                });
            }
            else
            {
                _ = Task.Run(async () => {
                    await new Signalling().Start();
                });
            }

            await StartCommandService();
            CTS.Dispose();
        }



        public async Task StartCommandService()
        {
            while (true)
            {
                var input = Console.ReadLine();
                if (string.IsNullOrEmpty(input))
                    continue;

                if (input.ToLower() == "exit")
                {
                    CTS.Cancel();
                    Logger.LogToConsole(LogType.Warn, $"Shutting down server. Closing window in 5 seconds.", nameof(MainEntry));
                    Console.Title = $"VoiceCraft - {Version}: Shutting Down...";
                    ServerEvents.InvokeStopping();
                    await Task.Delay(5000);
                    break;
                }

                var splitCmd = input.Split(' ');
                var cmd = splitCmd[0].ToLower();

                try
                {
                    switch (cmd)
                    {
                        case "list":
                            var p1 = ServerData.Participants;
                            Logger.LogToConsole(LogType.Info, $"Connected Participants {p1.Count}", nameof(MainEntry));
                            //Thread safety
                            Parallel.ForEach(p1, participant =>
                            {
                                Logger.LogToConsole(LogType.Info, $"Key: {participant.Key}, Binded: {participant.Value?.Binded}, IsMuted: {participant.Value?.Muted}, Name: {participant.Value?.MinecraftData.Gamertag}, Dimension: {participant.Value?.MinecraftData.DimensionId}, Position: {participant.Value?.MinecraftData.Position}, Rotation: {participant.Value?.MinecraftData.Rotation}", nameof(MainEntry));
                            });
                            break;
                        case "mute":
                            string muteKeyArg = splitCmd.ElementAt(1);
                            if (string.IsNullOrWhiteSpace(muteKeyArg))
                            {
                                Logger.LogToConsole(LogType.Error, "Error. Key argument cannot be empty!", nameof(MainEntry));
                                break;
                            }

                            _ = ushort.TryParse(muteKeyArg, out ushort keyArgUshort);
                            var p2 = ServerData.GetParticipantByKey(keyArgUshort);

                            if (p2 == null)
                            {
                                Logger.LogToConsole(LogType.Error, $"Error. Could not find participant {keyArgUshort}.", nameof(MainEntry));
                                break;
                            }

                            p2.Muted = true;
                            Logger.LogToConsole(LogType.Success, $"Successfully muted participant {keyArgUshort}.", nameof(MainEntry));
                            break;
                        case "unmute":
                            string unmuteKeyArg = splitCmd.ElementAt(1);
                            if (string.IsNullOrWhiteSpace(unmuteKeyArg))
                            {
                                Logger.LogToConsole(LogType.Error, "Error. Key argument cannot be empty!", nameof(MainEntry));
                                break;
                            }

                            _ = ushort.TryParse(unmuteKeyArg, out ushort keyArgUshort1);
                            var p3 = ServerData.GetParticipantByKey(keyArgUshort1);

                            if (p3 == null)
                            {
                                Logger.LogToConsole(LogType.Error, $"Error. Could not find participant {keyArgUshort1}.", nameof(MainEntry));
                                break;
                            }

                            p3.Muted = false;
                            Logger.LogToConsole(LogType.Success, $"Successfully unmuted participant {keyArgUshort1}.", nameof(MainEntry));
                            break;
                        case "kick":
                            string kickKeyArg = splitCmd.ElementAt(1);
                            if (string.IsNullOrWhiteSpace(kickKeyArg))
                            {
                                Logger.LogToConsole(LogType.Error, "Error. Key argument cannot be empty!", nameof(MainEntry));
                                break;
                            }

                            _ = ushort.TryParse(kickKeyArg, out ushort keyArgUshort2);
                            var p4 = ServerData.GetParticipantByKey(keyArgUshort2);

                            if (p4 == null)
                            {
                                Logger.LogToConsole(LogType.Error, $"Error. Could not find participant {keyArgUshort2}.", nameof(MainEntry));
                                break;
                            }

                            ServerData.RemoveParticipant(keyArgUshort2, "kicked");
                            Logger.LogToConsole(LogType.Success, $"Successfully kicked participant {keyArgUshort2}.", nameof(MainEntry));
                            break;
                        case "ban":
                            string banKeyArg = splitCmd.ElementAt(1);
                            if (string.IsNullOrWhiteSpace(banKeyArg))
                            {
                                Logger.LogToConsole(LogType.Error, "Error. Key argument cannot be empty!", nameof(MainEntry));
                                break;
                            }

                            _ = ushort.TryParse(banKeyArg, out ushort keyArgUshort3);
                            var p5 = ServerData.GetParticipantByKey(keyArgUshort3);

                            if (p5 == null)
                            {
                                Logger.LogToConsole(LogType.Error, $"Error. Could not find participant {keyArgUshort3}.", nameof(MainEntry));
                                break;
                            }

                            ServerProperties.BanIp(p5.SocketData.SignallingAddress?.ToString()?.Split(':').FirstOrDefault());
                            ServerData.RemoveParticipant(keyArgUshort3, "banned");
                            Logger.LogToConsole(LogType.Success, $"Successfully banned participant {keyArgUshort3}.", nameof(MainEntry));
                            break;
                        case "unban":
                            string unbanIpArg = splitCmd.ElementAt(1);
                            if (string.IsNullOrWhiteSpace(unbanIpArg))
                            {
                                Logger.LogToConsole(LogType.Error, "Error. Key argument cannot be empty!", nameof(MainEntry));
                                break;
                            }

                            var p6 = ServerProperties.Banlist.IPBans.FirstOrDefault(x => x == unbanIpArg);

                            if (p6 == null)
                            {
                                Logger.LogToConsole(LogType.Error, $"Error. Could not find IP {unbanIpArg}.", nameof(MainEntry));
                                break;
                            }

                            ServerProperties.UnbanIp(p6);
                            Logger.LogToConsole(LogType.Success, $"Successfully unbanned participant {unbanIpArg}.", nameof(MainEntry));
                            break;
                        case "banlist":
                            Logger.LogToConsole(LogType.Info, $"Banned IP's: {ServerProperties.Banlist.IPBans.Count}", nameof(MainEntry));
                            foreach (var ip in ServerProperties.Banlist.IPBans)
                            {
                                Logger.LogToConsole(LogType.Info, ip, nameof(MainEntry));
                            }
                            break;
                        case "setproximity":
                            var proxArg = splitCmd.ElementAt(1);
                            if (string.IsNullOrWhiteSpace(proxArg))
                            {
                                Logger.LogToConsole(LogType.Error, "Error. Distance argument cannot be empty!", nameof(MainEntry));
                                break;
                            }

                            _ = int.TryParse(proxArg, out int proxArgInt);

                            if (proxArgInt <= 0 || proxArgInt > 60)
                            {
                                Logger.LogToConsole(LogType.Error, "Error. Distance argument must be higher than 0 or lower than 61!", nameof(MainEntry));
                                break;
                            }

                            ServerProperties.Properties.ProximityDistance = proxArgInt;
                            Logger.LogToConsole(LogType.Success, $"Successfully set proximity distance to {proxArgInt}", nameof(MainEntry));
                            break;
                        case "toggleproximity":
                            var toggleArg = splitCmd.ElementAt(1);
                            if (string.IsNullOrWhiteSpace(toggleArg))
                            {
                                Logger.LogToConsole(LogType.Error, "Error. Toggle argument cannot be empty!", nameof(MainEntry));
                                break;
                            }

                            _ = bool.TryParse(toggleArg, out bool toggleArgBool);

                            ServerProperties.Properties.ProximityToggle = toggleArgBool;
                            Logger.LogToConsole(LogType.Success, $"Successfully set proximity toggle to {toggleArgBool}", nameof(MainEntry));
                            break;
                        case "setmotd":
                            var motdArg = splitCmd.ElementAt(1);
                            if (string.IsNullOrWhiteSpace(motdArg))
                            {
                                Logger.LogToConsole(LogType.Error, "Error. MOTD argument cannot be empty!", nameof(MainEntry));
                                break;
                            }

                            if (motdArg.Length > 30)
                            {
                                Logger.LogToConsole(LogType.Error, "Error. MOTD argument cannot be longer than 30 characters!", nameof(MainEntry));
                                break;
                            }

                            ServerProperties.Properties.ServerMOTD = motdArg;
                            Logger.LogToConsole(LogType.Success, $"Successfully set MOTD message to {motdArg}", nameof(MainEntry));
                            break;
                        case "toggleeffects":
                            var effectsArg = splitCmd.ElementAt(1);
                            if(string.IsNullOrWhiteSpace(effectsArg))
                            {
                                Logger.LogToConsole(LogType.Error, "Error. Toggle argument cannot be empty!", nameof(MainEntry));
                                break;
                            }

                            _ = bool.TryParse(effectsArg, out bool effectsArgBool);

                            ServerProperties.Properties.VoiceEffects = effectsArgBool;
                            Logger.LogToConsole(LogType.Success, $"Successfully set voice effects toggle to {effectsArgBool}", nameof(MainEntry));
                            break;
                        case "help":
                            Logger.LogToConsole(LogType.Info, "exit: Shuts down the server.", nameof(MainEntry));
                            Logger.LogToConsole(LogType.Info, "mute [key: ushort]: Mutes a participant.", nameof(MainEntry));
                            Logger.LogToConsole(LogType.Info, "unmute [key: ushort]: Unmutes a participant.", nameof(MainEntry));
                            Logger.LogToConsole(LogType.Info, "kick [key: ushort]: Kicks a participant.", nameof(MainEntry));
                            Logger.LogToConsole(LogType.Info, "ban [key: ushort]: Bans a participant. Doesn't blacklist the key but blacklists the participant's IP address", nameof(MainEntry));
                            Logger.LogToConsole(LogType.Info, "unban [ipAddress: string]: Unbans an IPAddress", nameof(MainEntry));
                            Logger.LogToConsole(LogType.Info, "banlist: Shows the ip addresses that are banned.", nameof(MainEntry));
                            Logger.LogToConsole(LogType.Info, "setproximity [distance: int]: Sets the proximity distance (Defaults to serverProperties.json setting on server restart).", nameof(MainEntry));
                            Logger.LogToConsole(LogType.Info, "toggleproximity [toggle: boolean]: Switches proximity chat on or off. If off, then it becomes a regular voice chat. (Defaults to serverProperties.json setting on server restart).", nameof(MainEntry));
                            Logger.LogToConsole(LogType.Info, "setmotd [MOTD: string]: Sets the servers MOTD message. (Defaults to serverProperties.json setting on server restart)", nameof(MainEntry));
                            Logger.LogToConsole(LogType.Info, "toggleeffects [toggle: boolean]: Switches the voice effects on or off. (Defaults to serverProperties.json setting on server restart)", nameof(MainEntry));
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
    }
}
