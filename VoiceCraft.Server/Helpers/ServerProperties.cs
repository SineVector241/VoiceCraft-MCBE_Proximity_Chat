using Newtonsoft.Json;
using System.Net;

namespace VoiceCraft.Server.Helpers
{
    public static class ServerProperties
    {
        public static PropertiesData Properties { get; private set; } = new PropertiesData();
        public static BanlistData Banlist { get; private set; } = new BanlistData();

        const string PropertiesFile = "ServerProperties.json";
        const string BanlistFile = "Banlist.json";

        public static void LoadProperties()
        {
            try
            {
                //Checking and setting properties.
                if (!File.Exists(PropertiesFile))
                {
                    Logger.LogToConsole(LogType.Warn, $"{PropertiesFile} file does not exist. Creating file.", nameof(Helpers.ServerProperties));
                    string jsonString = JsonConvert.SerializeObject(Properties, Formatting.Indented);
                    File.WriteAllText(PropertiesFile, jsonString);
                    Logger.LogToConsole(LogType.Success, $"Successfully created file {PropertiesFile}.", nameof(Helpers.ServerProperties));
                }
                else
                {
                    string jsonString = File.ReadAllText(PropertiesFile);
                    var properties = JsonConvert.DeserializeObject<PropertiesData>(jsonString);
                    if (properties != null)
                        Properties = properties;
                    else
                        Logger.LogToConsole(LogType.Warn, $"Failed to parse {PropertiesFile}. Falling back to default properties.", nameof(Helpers.ServerProperties));
                }

                //Checking and setting banlist.
                if (!File.Exists(BanlistFile))
                {
                    Logger.LogToConsole(LogType.Warn, $"{BanlistFile} file does not exist. Creating file.", nameof(Helpers.ServerProperties));
                    string jsonString = JsonConvert.SerializeObject(Banlist, Formatting.Indented);
                    File.WriteAllText(BanlistFile, jsonString);
                    Logger.LogToConsole(LogType.Success, $"Successfully created file {BanlistFile}.", nameof(Helpers.ServerProperties));
                }
                else
                {
                    string jsonString = File.ReadAllText(BanlistFile);
                    var banlist = JsonConvert.DeserializeObject<BanlistData>(jsonString);
                    if (banlist != null)
                        Banlist = banlist;
                    else
                        Logger.LogToConsole(LogType.Warn, $"Failed to parse {BanlistFile}. Falling back to default properties.", nameof(Helpers.ServerProperties));
                }


            }
            catch (Exception ex)
            {
                ServerEvents.InvokeFailed(nameof(ServerProperties), ex.Message);
                return;
            }

            if (Properties.SignallingPortUDP == Properties.VoicePortUDP)
            {
                ServerEvents.InvokeFailed(nameof(ServerProperties), "SignallingPort and VoicePort cannot be the same!");
                return;
            }

            if (Properties.SignallingPortUDP < 1025 || Properties.VoicePortUDP < 1025 || Properties.MCCommPortTCP < 1025)
            {
                ServerEvents.InvokeFailed(nameof(ServerProperties), "One of the ports is set lower than 1025. Ports must be set at or between 1025 to 65535!");
                return;
            }

            if(string.IsNullOrWhiteSpace(Properties.PermanentServerKey))
            {
                Logger.LogToConsole(LogType.Warn, "Permanent server key not set or empty. Generating temporary key.", nameof(ServerProperties));
                Properties.PermanentServerKey = Guid.NewGuid().ToString();
            }

            if(string.IsNullOrWhiteSpace(Properties.ServerMOTD))
            {
                Logger.LogToConsole(LogType.Warn, "Server MOTD is not set. Setting to default message.", nameof(ServerProperties));
                Properties.ServerMOTD = "VoiceCraft Proximity Chat!";
            }

            if(Properties.ServerMOTD.Length > 30)
            {
                Logger.LogToConsole(LogType.Warn, "Server MOTD is longer than 30 characters!. Setting to default message.", nameof(ServerProperties));
                Properties.ServerMOTD = "VoiceCraft Proximity Chat!";
            }

            ServerEvents.InvokeStarted(nameof(ServerProperties));
        }

        public static void BanIp(string? Ip)
        {
            if (string.IsNullOrWhiteSpace(Ip))
            {
                Logger.LogToConsole(LogType.Error, "Error. Participant IPAddress could not be determined!", nameof(MainEntry));
                return;
            }

            Banlist.IPBans.Add(Ip);
            string banlistJson = JsonConvert.SerializeObject(Banlist, Formatting.Indented);
            File.WriteAllText(BanlistFile, banlistJson);
        }

        public static void UnbanIp(string? Ip)
        {
            var ip = Banlist.IPBans.FirstOrDefault(x => x == Ip);
            if (ip == null)
            {
                Logger.LogToConsole(LogType.Error, "Error. IPAddress does not exist in the ban list!", nameof(ServerProperties));
                return;
            }

            Banlist.IPBans.Remove(ip);
            string banlistJson = JsonConvert.SerializeObject(Banlist, Formatting.Indented);
            File.WriteAllText(BanlistFile, banlistJson);

            Logger.LogToConsole(LogType.Success, "Successfully unbanned IPAddress!", nameof(ServerProperties));
        }
    }

    public class PropertiesData
    {
        //UDP Ports
        public ushort SignallingPortUDP { get; set; } = 9050;
        public ushort VoicePortUDP { get; set; } = 9051;

        //TCP Ports
        public ushort MCCommPortTCP { get; set; } = 9050;

        //Unchangeable Settings
        public string PermanentServerKey { get; set; } = "";
        public AudioCodecs Codec { get; set; } = AudioCodecs.Opus;
        public ConnectionTypes ConnectionType { get; set; } = ConnectionTypes.Server;

        //Changeable Settings
        public int ProximityDistance { get; set; } = 30;
        public bool ProximityToggle { get; set; } = true;
        public string ServerMOTD { get; set; } = "VoiceCraft Proximity Chat!";
    }

    public class BanlistData
    {
        public List<string> IPBans { get; set; } = new List<string>();
    }

    public enum AudioCodecs
    {
        Opus,
        G722,
        Hybrid
    }

    public enum ConnectionTypes
    {
        Server,
        Client,
        Hybrid
    }
}
