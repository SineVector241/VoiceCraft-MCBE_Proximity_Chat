using Newtonsoft.Json;
using VoiceCraft.Core.Server;

namespace VoiceCraft.Server
{
    public class ServerData
    {
        const string PropertiesDirectory = "ServerProperties.json";
        const string BanlistDirectory = "Banlist.json";

        private Properties ServerProperties { get; set; } = new Properties();
        private Banlist BanlistData { get; set; } = new Banlist();

        public Properties LoadProperties()
        {
            //Load properties files and create if not exists.
            if (!File.Exists(PropertiesDirectory))
            {
                Logger.LogToConsole(LogType.Warn, $"{PropertiesDirectory} file does not exist. Creating file...", "Properties");
                string jsonString = JsonConvert.SerializeObject(ServerProperties, Formatting.Indented);
                File.WriteAllText(PropertiesDirectory, jsonString);
                Logger.LogToConsole(LogType.Success, $"Successfully created file {PropertiesDirectory}.", "Properties");
            }
            else
            {
                Logger.LogToConsole(LogType.Info, "Loading properties...", "Properties");
                string jsonString = File.ReadAllText(PropertiesDirectory);
                var properties = JsonConvert.DeserializeObject<Properties>(jsonString);
                if (properties != null)
                    ServerProperties = properties;
                else
                    Logger.LogToConsole(LogType.Warn, $"Failed to parse {PropertiesDirectory}. Falling back to default properties.", "Properties");
            }

            if(ServerProperties.MCCommPortTCP == ServerProperties.SignallingPortTCP)
                throw new Exception("MCComm and Signalling port cannot be identical!");
            if (ServerProperties.SignallingPortTCP < 1025 || ServerProperties.VoicePortUDP < 1025 || ServerProperties.MCCommPortTCP < 1025)
                throw new Exception("One of the ports is lower than the minimum port 1025!");
            if (ServerProperties.SignallingPortTCP > 65535 || ServerProperties.VoicePortUDP > 65535 || ServerProperties.MCCommPortTCP > 65535)
                throw new Exception("One of the ports is higher than the maximum port 65535!");
            if (ServerProperties.ServerMOTD.Length > 30)
                throw new Exception("Server MOTD cannot be longer than 30 characters!");
            if (ServerProperties.ProximityDistance > 120 || ServerProperties.ProximityDistance < 1)
                throw new Exception("Proximity distance can only be between 1 and 120!");

            if (string.IsNullOrWhiteSpace(ServerProperties.PermanentServerKey))
            {
                Logger.LogToConsole(LogType.Warn, "Permanent server key not set or empty. Generating temporary key.", "Properties");
                ServerProperties.PermanentServerKey = Guid.NewGuid().ToString();
            }

            if (string.IsNullOrWhiteSpace(ServerProperties.ServerMOTD))
            {
                Logger.LogToConsole(LogType.Warn, "Server MOTD is not set. Setting to default message.", "Properties");
                ServerProperties.ServerMOTD = "VoiceCraft Proximity Chat!";
            }

            Logger.LogToConsole(LogType.Success, "Loaded properties successfully!", "Properties");

            return ServerProperties;
        }

        public Banlist LoadBanlist()
        {
            //Load banlist files and create if not exists.
            if (!File.Exists(BanlistDirectory))
            {
                Logger.LogToConsole(LogType.Warn, $"{BanlistDirectory} file does not exist. Creating file...", "Banlist");
                string jsonString = JsonConvert.SerializeObject(BanlistData, Formatting.Indented);
                File.WriteAllText(BanlistDirectory, jsonString);
                Logger.LogToConsole(LogType.Success, $"Successfully created file {BanlistDirectory}.", "Banlist");
            }
            else
            {
                Logger.LogToConsole(LogType.Info, "Loading banlist...", "Banlist");
                string jsonString = File.ReadAllText(BanlistDirectory);
                var properties = JsonConvert.DeserializeObject<Banlist>(jsonString);
                if (properties != null)
                    BanlistData = properties;
                else
                    Logger.LogToConsole(LogType.Warn, $"Failed to parse {BanlistDirectory}. Falling back to default banlist.", "Banlist");
            }

            Logger.LogToConsole(LogType.Success, "Loaded banlist successfully!", "Banlist");

            return BanlistData;
        }
    }
}
