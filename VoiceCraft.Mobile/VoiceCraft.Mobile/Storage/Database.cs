using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using VoiceCraft.Mobile.Models;

namespace VoiceCraft.Mobile.Storage
{
    public static class Database
    {
        static object objClass = new object();

        const string DbFile = "Database.json";
        static string DatabasePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), DbFile);
        static DatabaseModel DBData = new DatabaseModel();

        //Events
        public delegate void ServerAdd(ServerModel Server);
        public delegate void ServerRemove(ServerModel Server);

        public static event ServerAdd OnServerAdd;
        public static event ServerRemove OnServerRemove;

        public static List<ServerModel> GetServers()
        {
            LoadDatabase();
            return DBData.Servers;
        }

        public static ServerModel GetServerByName(string Name)
        {
            var servers = GetServers();
            var server = servers.FirstOrDefault(x => x.Name == Name);
            if (server == null) throw new Exception($"Could not find server {Name}.");
            return server;
        }

        public static void AddServer(ServerModel server)
        {
            if (string.IsNullOrWhiteSpace(server.Name)) throw new Exception("Name cannot be empty!");
            else if (string.IsNullOrEmpty(server.IP)) throw new Exception("IP cannot be empty!");
            else if (server.Port < 1025) throw new Exception("Port cannot be lower than 1025");
            else if (server.Port > 65535) throw new Exception("Port cannot be higher than 65535");
            else if (server.Codec == -1) throw new Exception("A codec must be selected!");
            else if (GetServers().Exists(x => x.Name == server.Name)) throw new Exception("Name already exists! Name must be unique!");
            DBData.Servers.Add(server);
            OnServerAdd?.Invoke(server);
            SaveDatabase();
        }

        public static void DeleteServer(ServerModel server)
        {
            DBData.Servers.Remove(server);
            OnServerRemove?.Invoke(server);
            SaveDatabase();
        }

        public static void SetPassableObject(object obj)
        {
            objClass = obj;
        }

        public static T GetPassableObject<T>()
        {
            return (T)objClass;
        }

        //Private Methods
        private static void LoadDatabase()
        {
            if (!File.Exists(DatabasePath))
            {
                File.WriteAllText(DatabasePath, JsonConvert.SerializeObject(DBData));
                return;
            }
            DBData = JsonConvert.DeserializeObject<DatabaseModel>(File.ReadAllText(DatabasePath));
            //Make sure the application doesn't crash.
            if(DBData.Servers == null || DBData.Settings == null) DBData = new DatabaseModel();
        }

        private static void SaveDatabase()
        {
            File.WriteAllText(DatabasePath, JsonConvert.SerializeObject(DBData));
        }
    }
}
