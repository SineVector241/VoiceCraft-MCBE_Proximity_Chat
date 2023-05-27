using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using VoiceCraft_Server.Data;

namespace VoiceCraft_Server
{
    public class ServerProperties
    {
        const string _propertiesFile = "serverProperties.json";
        const string _banlistFile = "banlist.json";
        public static Properties _serverProperties;
        public static BanList _banlist;
        public ServerProperties()
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

                if (!File.Exists(_banlistFile))
                {
                    Logger.LogToConsole(LogType.Warn, "banlist.json file does not exist. Creating file...", nameof(ServerProperties));
                    _banlist = new BanList();
                    string banlistJson = JsonConvert.SerializeObject(_banlist, Formatting.Indented);
                    File.WriteAllText(_banlistFile, banlistJson);
                    Logger.LogToConsole(LogType.Success, "Created banlist.json", nameof(ServerProperties));
                }
                else
                {
                    string banlistJson = File.ReadAllText(_banlistFile);
                    _banlist = JsonConvert.DeserializeObject<BanList>(banlistJson);
                }
            }
            catch (Exception ex)
            {
                ServerEvents.InvokeFailed(nameof(ServerProperties), ex.Message);
                return;
            }

            if(_serverProperties.SignallingPort_UDP == _serverProperties.VoicePort_UDP)
            {
                ServerEvents.InvokeFailed(nameof(ServerProperties), "SignallingPort and VoicePort cannot be the same!");
                return;
            }

            ServerEvents.InvokeStarted(nameof(ServerProperties));
        }

        public static void BanIp(string IpAddress)
        {
            if (string.IsNullOrWhiteSpace(IpAddress))
            {
                Logger.LogToConsole(LogType.Error, "Error. Participant IPAddress could not be determined!", nameof(MainEntry));
                return;
            }

            _banlist.BannedIPs.Add(IpAddress);
            string banlistJson = JsonConvert.SerializeObject(_banlist, Formatting.Indented);
            File.WriteAllText(_banlistFile, banlistJson);
        }

        public static void UnbanIp(string IpAddress)
        {
            var ip = _banlist.BannedIPs.FirstOrDefault(x => x == IpAddress);
            if(ip == null)
            {
                Logger.LogToConsole(LogType.Error, "Error. IPAddress does not exist in the ban list!", nameof(ServerProperties));
                return;
            }

            _banlist.BannedIPs.Remove(ip);
            string banlistJson = JsonConvert.SerializeObject(_banlist, Formatting.Indented);
            File.WriteAllText(_banlistFile, banlistJson);

            Logger.LogToConsole(LogType.Success, "Successfully unbanned IPAddress!", nameof(ServerProperties));
        }
    }

    public class Properties
    {
        //UDP Ports
        public int SignallingPort_UDP { get; set; } = 9050;
        public int VoicePort_UDP { get; set; } = 9051;
        
        //TCP Ports
        public int MCCommPort_TCP { get; set; } = 9050;

        //Other Settings
        public int ProximityDistance { get; set; } = 30;
        public string PermanentServerKey { get; set; } = "";
        public bool ProximityToggle { get; set; } = true;
    }

    public class BanList
    {
        //Banlist
        public List<string> BannedIPs { get; set; } = new List<string>();
    }
}
