using Newtonsoft.Json;
using VoiceCraft.Maui.Models;

namespace VoiceCraft.Maui.Services
{
    public class Database
    {
        private const string ServerDb = "Servers.json";
        private const string SettingsDb = "Settings.json";

        private string ServersDbPath = Path.Combine(FileSystem.Current.AppDataDirectory, ServerDb);
        private string SettingsDbPath = Path.Combine(FileSystem.Current.AppDataDirectory, SettingsDb);

        public delegate void ServerAdded(ServerModel server);
        public delegate void ServerRemoved(ServerModel server);

        public event ServerAdded? OnServerAdded;
        public event ServerRemoved? OnServerRemoved;

        public SettingsModel Settings { get; } = new SettingsModel();
        public List<ServerModel> Servers { get; } = new List<ServerModel>();
        public Database()
        {
            if (!File.Exists(ServersDbPath))
            {
                File.WriteAllText(ServersDbPath, JsonConvert.SerializeObject(Servers));
            }
            else
            {
                var ReadDBData = JsonConvert.DeserializeObject<List<ServerModel>>(File.ReadAllText(ServersDbPath));
                if (ReadDBData != null)
                    Servers = ReadDBData;
            }


            if (!File.Exists(SettingsDbPath))
            {
                File.WriteAllText(SettingsDbPath, JsonConvert.SerializeObject(Settings));
            }
            else
            {
                var ReadDBData = JsonConvert.DeserializeObject<SettingsModel>(File.ReadAllText(SettingsDbPath));
                if (ReadDBData != null)
                    Settings = ReadDBData;
            }
        }

        public async Task AddServer(ServerModel server)
        {
            if (string.IsNullOrWhiteSpace(server.Name)) throw new Exception("Name cannot be empty!");
            else if (string.IsNullOrEmpty(server.IP)) throw new Exception("IP cannot be empty!");
            else if (server.Port < 1025) throw new Exception("Port cannot be lower than 1025");
            else if (server.Port > 65535) throw new Exception("Port cannot be higher than 65535");
            else if (Servers.Exists(x => x.Name == server.Name)) throw new Exception("Name already exists! Name must be unique!");
            Servers.Add(server);
            OnServerAdded?.Invoke(server);
            await SaveServers();
        }

        public async Task EditServer(ServerModel server)
        {
            var foundServer = Servers.FirstOrDefault(x => x.Name == server.Name);
            if(foundServer != null)
            {
                if (string.IsNullOrWhiteSpace(server.Name)) throw new Exception("Name cannot be empty!");
                else if (string.IsNullOrEmpty(server.IP)) throw new Exception("IP cannot be empty!");
                else if (server.Port < 1025) throw new Exception("Port cannot be lower than 1025");
                else if (server.Port > 65535) throw new Exception("Port cannot be higher than 65535");

                foundServer.IP = server.IP;
                foundServer.Port = server.Port;
                foundServer.Key = server.Key;
                await SaveServers();
                return; //UI updates automatically as soon as we change a value.
            }
            throw new Exception("Server not found!");
        }

        public async Task RemoveServer(ServerModel server)
        {
            Servers.Remove(server);
            OnServerRemoved?.Invoke(server);
            await SaveServers();
        }

        public async Task SaveAllAsync()
        {
            await SaveServers();
            await SaveSettings();
        }

        public async Task SaveServers()
        {
            await File.WriteAllTextAsync(ServersDbPath, JsonConvert.SerializeObject(Servers, Formatting.Indented));
        }

        public async Task SaveSettings()
        {
            await File.WriteAllTextAsync(SettingsDbPath, JsonConvert.SerializeObject(Settings, Formatting.Indented));
        }

        public static Database Instance { get; } = new Database();
    }
}
