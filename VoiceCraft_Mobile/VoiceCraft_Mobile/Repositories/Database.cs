using System.Collections.Generic;
using System.IO;
using VoiceCraft_Mobile.Models;
using Xamarin.Essentials;
using Newtonsoft.Json;
using System;

namespace VoiceCraft_Mobile.Repositories
{
    public class Database
    {
        private const string _dbFolder = "Databases";
        private static string _serversDbFile = "Servers.json";
        private static string _dbPath;

        static Database()
        {
            _dbPath = GetLocalFileDirectory();
            _serversDbFile = Path.Combine(_dbPath, _serversDbFile);
        }

        public static void AddServer(ServerModel server)
        {
            ServersDataModel data = new ServersDataModel();
            if (File.Exists(_serversDbFile))
            {
                var json = File.ReadAllText(_serversDbFile);
                data = JsonConvert.DeserializeObject<ServersDataModel>(json);
            }

            server.LocalId = Guid.NewGuid().ToString();
            data.Servers.Add(server);

            var serialized = JsonConvert.SerializeObject(data);
            File.WriteAllText(_serversDbFile, serialized);
        }

        public static void EditServer(ServerModel server)
        {
            ServersDataModel data = new ServersDataModel();
            if (File.Exists(_serversDbFile))
            {
                var json = File.ReadAllText(_serversDbFile);
                data = JsonConvert.DeserializeObject<ServersDataModel>(json);
            }

            var found = data.Servers.FindIndex(x => x.LocalId == server.LocalId);
            if (found != -1)
            {
                data.Servers[found] = server;
            }
            else
                return;

            var serialized = JsonConvert.SerializeObject(data);
            File.WriteAllText(_serversDbFile, serialized);
        }

        public static void DeleteServer(string localId)
        {
            ServersDataModel data = new ServersDataModel();
            if (File.Exists(_serversDbFile))
            {
                var json = File.ReadAllText(_serversDbFile);
                data = JsonConvert.DeserializeObject<ServersDataModel>(json);
            }

            data.Servers.RemoveAll(x => x.LocalId == localId);

            var serialized = JsonConvert.SerializeObject(data);
            File.WriteAllText(_serversDbFile, serialized);
        }

        public static List<ServerModel> GetServers()
        {
            ServersDataModel data = new ServersDataModel();
            if (File.Exists(_serversDbFile))
            {
                var json = File.ReadAllText(_serversDbFile);
                data = JsonConvert.DeserializeObject<ServersDataModel>(json);
            }

            return data.Servers;
        }

        private static string GetLocalFileDirectory()
        {
            var docFolder = FileSystem.AppDataDirectory;
            var libFolder = Path.Combine(docFolder, _dbFolder);

            if (!Directory.Exists(libFolder))
            {
                Directory.CreateDirectory(libFolder);
            }
            return libFolder;
        }
    }

    public class ServersDataModel
    {
        public List<ServerModel> Servers { get; set; } = new List<ServerModel>();
    }
}