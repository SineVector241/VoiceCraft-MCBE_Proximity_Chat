using System.Net;
using System.Numerics;
using VoiceCraftProximityChat_Server.Servers;

namespace VoiceCraftProximityChat_Server.Dependencies
{
    public class ServerData
    {
        public List<SessionKey> SessionKeys { get; set; } = new List<SessionKey>();
        public List<Client> ClientList { get; set; } = new List<Client>();
        public event EventHandler<ClientConnectEventArgs> ClientConnect;
        public event EventHandler<ClientDisconnectEventArgs> ClientDisconnect;

        private Timer TimeoutSessionChecker = null;

        public ServerData()
        {
            //Developer Keys
            SessionKeys.Add(new SessionKey() { Key = "hy67a", PlayerId = "EEEE", RegisteredAt = DateTime.UtcNow.AddMinutes(5), Username = "Test1" });
            SessionKeys.Add(new SessionKey() { Key = "x456j", PlayerId = "EEEEE", RegisteredAt = DateTime.UtcNow.AddMinutes(5), Username = "Test2" });
            SessionKeys.Add(new SessionKey() { Key = "x456i", PlayerId = "EEEEEA", RegisteredAt = DateTime.UtcNow.AddMinutes(5), Username = "Test3" });

            TimeoutSessionChecker = new Timer(new TimerCallback(ClearTimeoutSessions), null, 0, 2000);
        }

        //Public Methods
        public string? CreateNewSessionKey(string PlayerId, string Username)
        {
            if (ClientList.Exists(x => x.PlayerId == PlayerId) || SessionKeys.Exists(x => x.PlayerId == PlayerId))
                return null;

            var sessionKey = new SessionKey() { PlayerId = PlayerId, Key = GenerateKey(), Username = Username };
            SessionKeys.Add(sessionKey);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[HTTP] Created Session Key: Key: {sessionKey.Key} | Player Id: {PlayerId}");
            Console.ResetColor();

            return sessionKey.Key;
        }

        public bool ClientLogin(string SessionKey, EndPoint endPoint)
        {
            var key = SessionKeys.FirstOrDefault(x => x.Key == SessionKey);
            if(key != null)
            {
                SessionKeys.Remove(key);
                ClientList.Add(new Client() { Location = new Vector3(), endPoint = endPoint, Key = SessionKey, PlayerId = key.PlayerId, lastPing = DateTime.UtcNow, Username = key.Username });

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"[ServerData] Accepted login request: Key: {SessionKey} | Address: {endPoint}");
                Console.ResetColor();

                //Fire Event
                OnClientConnect(new ClientConnectEventArgs() { SessionKey = key.Key, Username = key.Username, iPEndPoint = endPoint });
                return true;
            }

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[ServerData] Denied login request: Key: {SessionKey} | Address: {endPoint}");
            Console.ResetColor();
            return false;
        }

        public Client? GetClientBySessionKey(string Key)
        {
            return ClientList.FirstOrDefault(x => x.Key == Key);
        }

        public void UpdateClient(Client client)
        {
            var data = ClientList.FirstOrDefault(x => x.Key == client.Key);
            if (data != null)
                data = client;
        }

        public void UpdateClientList(List<Player> players)
        {
            foreach (var client in ClientList)
            {
                var player = players.FirstOrDefault(x => x.PlayerId == client.PlayerId);
                if (player != null)
                {
                    client.Location = player.Location;
                }
            }
        }

        //Private Methods
        private string GenerateKey()
        {
            Random res = new Random();
            string str = "abcdefghijklmnopqrstuvwxyz0123456789";
            int size = 5;

            string RandomString = "";

            for (int i = 0; i < size; i++)
            {
                int x = res.Next(str.Length);
                RandomString += str[x];
            }

            return RandomString;
        }

        private void ClearTimeoutSessions(object? state)
        {
            for(int i = 0; i < ClientList.Count; i++)
            {
                if((DateTime.UtcNow - ClientList[i].lastPing).Seconds > 10)
                {
                    OnClientDisconnect(new ClientDisconnectEventArgs() { SessionKey = ClientList[i].Key, Username = ClientList[i].Username });
                    ClientList.RemoveAt(i);
                }
            }

            for (int i = 0; i < SessionKeys.Count; i++)
            {
                if ((DateTime.UtcNow - SessionKeys[i].RegisteredAt).Seconds > 0)
                {
                    Console.WriteLine($"[ServerData] Removed Key: Key: {SessionKeys[i].Key} - Timeout");
                    SessionKeys.RemoveAt(i);
                }
            }
        }

        //Event Methods
        protected virtual void OnClientConnect(ClientConnectEventArgs e)
        {
            var eventHandler = ClientConnect;
            eventHandler.Invoke(null, e);
        }

        protected virtual void OnClientDisconnect(ClientDisconnectEventArgs e)
        {
            var eventHandler = ClientDisconnect;
            eventHandler.Invoke(null, e);
        }

        //Public Instance
        public static ServerData Data = new ServerData();
    }

    //Data Structure
    public class Client
    {
        public EndPoint endPoint { get; set; }
        public string Key { get; set; } = "";
        public string PlayerId { get; set; } = "";
        public string Username { get; set; } = "";
        public bool isReady { get; set; }

        public Vector3 Location { get; set; }
        public DateTime lastPing { get; set; } = DateTime.UtcNow;
    }

    public class SessionKey
    {
        public string Key { get; set; } = "";
        public string PlayerId { get; set; } = "";
        public string Username { get; set; } = "";
        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow.AddMinutes(5);
    }

    //Event Args
    public class ClientConnectEventArgs: EventArgs
    {
        public string Username { get; set; } = "";
        public string SessionKey { get; set; } = "";
        public EndPoint iPEndPoint { get; set; }
    }

    public class ClientDisconnectEventArgs : EventArgs
    {
        public string Username { get; set; } = "";
        public string SessionKey { get; set; } = "";
    }
}
