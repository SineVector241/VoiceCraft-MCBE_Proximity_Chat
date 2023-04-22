using Newtonsoft.Json;
using System.IO;
using VoiceCraftProximityChat.Models;
using System;
using System.Linq;
using System.Diagnostics;

namespace VoiceCraftProximityChat.Storage
{
    public static class Database
    {
        private const string DbFolder = "./Databases";
        private const string ServersDb = "Servers.json";

        private static string ServersDbPath;

        static Database()
        {
            ServersDbPath = Path.Combine(DbFolder, ServersDb);
            if (!Directory.Exists(DbFolder))
            {
                Directory.CreateDirectory(DbFolder);
            }
        }

        public static ServerListModel GetServers()
        {
            ServerListModel servers = new ServerListModel();
            if (File.Exists(ServersDbPath))
            {
                var json = File.ReadAllText(ServersDbPath);
                servers = JsonConvert.DeserializeObject<ServerListModel>(json);
            }
            return servers;
        }

        public static void AddServer(ServerModel server)
        {
            var servers = GetServers();

            if(servers.Servers.Exists(x => x.Name == server.Name)) 
                throw new InvalidOperationException("Conflict detected! Multiple server objects cannot have the same name!");

            servers.Servers.Add(server);
            
            var serialized = JsonConvert.SerializeObject(servers);
            File.WriteAllText(ServersDbPath, serialized);
        }

        public static void DeleteServer(ServerModel server) 
        {
            var servers = GetServers();

            if (!servers.Servers.Exists(x => x.Name == server.Name))
                throw new InvalidOperationException("Cannot find server.");

            servers.Servers.RemoveAll(x => x.Name == server.Name);

            var serialized = JsonConvert.SerializeObject(servers);
            File.WriteAllText(ServersDbPath, serialized);
        }

        public static void EditServer(ServerModel server)
        {
            var servers = GetServers();

            var serverIndex = servers.Servers.FindIndex(x => x.Name == server.Name);
            if(serverIndex == -1)
                throw new InvalidOperationException("Cannot find server.");
            else
                servers.Servers[serverIndex] = server;

            var serialized = JsonConvert.SerializeObject(servers);
            File.WriteAllText(ServersDbPath, serialized);
        }

        public static ServerModel? GetServerByName(string name)
        {
            var servers = GetServers();

            return servers.Servers.FirstOrDefault(x => x.Name == name);
        }
    }
}
