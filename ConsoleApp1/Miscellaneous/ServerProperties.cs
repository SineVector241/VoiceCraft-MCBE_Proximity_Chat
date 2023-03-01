using Newtonsoft.Json;
using System;
using System.IO;

namespace VoiceCraft_Server
{
    public class ServerProperties
    {
        const string _propertiesFile = "serverProperties.json";
        public static Properties _serverProperties;
        static ServerProperties()
        {
            try
            {
                if (!File.Exists(_propertiesFile))
                {
                    Logger.LogToConsole(LogType.Warn, "serverProperties.json file does not exist. Creating file...", nameof(ServerProperties));
                    _serverProperties = new Properties();
                    string serverPropertiesJson = JsonConvert.SerializeObject(_serverProperties, Formatting.Indented);
                    File.WriteAllText(_propertiesFile, serverPropertiesJson);
                    Logger.LogToConsole(LogType.Success, "Created serverProperties.json", nameof(ServerProperties));
                }
                else
                {
                    string serverPropertiesJson = File.ReadAllText(_propertiesFile);
                    _serverProperties = JsonConvert.DeserializeObject<Properties>(serverPropertiesJson);
                }
            }
            catch (Exception ex)
            {
                Logger.LogToConsole(LogType.Error, ex.Message, nameof(ServerProperties));
            }
        }
    }

    public class Properties
    {
        //UDP Ports
        public int SignallingPort_UDP { get; set; } = 9050;
        public int VoicePort_UDP { get; set; } = 9051;
        
        //TCP Ports
        public int MCCommPort_TCP { get; set; } = 9050;
    }
}
