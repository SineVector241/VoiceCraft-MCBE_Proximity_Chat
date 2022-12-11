using System.Net;
using System.Net.Sockets;
using System.Numerics;

namespace VoiceCraftProximityChat_Server.Servers
{
    public class Server
    {
        private List<SessionKey> SessionKeys { get; set; } = new List<SessionKey>();
        private List<Client> clientList = new List<Client>();
        private Socket serverSocket;
        private Packet packets;
        private byte[] dataStream = new byte[4824];

        public void Setup(int Port)
        {
            //Developer Keys
            SessionKeys.Add(new SessionKey() { Key = "hy67a", PlayerId = "EEEE", RegisteredAt = DateTime.UtcNow.AddMinutes(5) });
            SessionKeys.Add(new SessionKey() { Key = "x456j", PlayerId = "EEEEE", RegisteredAt = DateTime.UtcNow.AddMinutes(5) });
            SessionKeys.Add(new SessionKey() { Key = "x456i", PlayerId = "EEEEEA", RegisteredAt = DateTime.UtcNow.AddMinutes(5) });

            packets = new Packet();
            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPEndPoint serverEp = new IPEndPoint(IPAddress.Any, Port);
            serverSocket.Bind(serverEp);
            IPEndPoint clients = new IPEndPoint(IPAddress.Any, 0);
            EndPoint endPoint = clients;
            serverSocket.BeginReceiveFrom(dataStream, 0, dataStream.Length, SocketFlags.None, ref endPoint, new AsyncCallback(ReceiveData), endPoint);

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"Started UDP Server: Port - {Port}");
            Console.ResetColor();
        }

        private void ReceiveData(IAsyncResult asyncResult)
        {
            try
            {
                // Initialise a packet object to store the received data
                Packet receivedData = new Packet(dataStream);

                // Initialise a packet object to store the data to be sent
                Packet sendData = new Packet();

                // Initialise the IPEndPoint for the clients
                IPEndPoint clients = new IPEndPoint(IPAddress.Any, 0);

                // Initialise the EndPoint for the clients
                EndPoint epSender = clients;

                // Receive all data
                serverSocket.EndReceiveFrom(asyncResult, ref epSender);
                serverSocket.BeginReceiveFrom(dataStream, 0, dataStream.Length, SocketFlags.None, ref epSender, new AsyncCallback(ReceiveData), epSender);
                ClearTimeoutSessions();

                switch (receivedData.VCPacketDataIdentifier)
                {
                    case PacketIdentifier.Login:
                        Console.WriteLine($"[UDP] Recieved new login request: Key: {receivedData.VCSessionKey} | Address: {epSender}");

                        if (SessionKeys.Exists(x => x.Key == receivedData.VCSessionKey))
                        {
                            var packet = new Packet() { VCPacketDataIdentifier = PacketIdentifier.Accept };
                            serverSocket.BeginSendTo(packet.GetPacketDataStream(), 0, packet.GetPacketDataStream().Length, SocketFlags.None, epSender, new AsyncCallback(SendData), epSender);

                            var key = SessionKeys.FirstOrDefault(x => x.Key == receivedData.VCSessionKey);
                            if (key != null)
                            {
                                SessionKeys.Remove(key);

                                clientList.Add(new Client() { Location = new Vector3(), endPoint = epSender, Key = receivedData.VCSessionKey, PlayerId = key.PlayerId, lastPing = DateTime.UtcNow });

                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine($"[UDP] Accepted login request: Key: {receivedData.VCSessionKey} | Address: {epSender}");
                                Console.ResetColor();
                            }
                            else
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine($"[UDP] Denied login request: Key: {receivedData.VCSessionKey} | Address: {epSender}");
                                Console.ResetColor();
                            }
                        }
                        break;

                    case PacketIdentifier.Ready:
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"[UDP] Recieved ReadyEvent: Key: {receivedData.VCSessionKey} | Address: {epSender}");
                        Console.ResetColor();

                        var VCClient = clientList.FirstOrDefault(x => x.Key == receivedData.VCSessionKey);
                        if (VCClient != null)
                        {
                            VCClient.isReady = true;
                        }
                        break;

                    case PacketIdentifier.AudioStream:
                        var AudioPacket = new Packet() { VCPacketDataIdentifier = PacketIdentifier.AudioStream, VCAudioBuffer = receivedData.VCAudioBuffer };
                        var volume = 0.0f;
                        Task.Factory.StartNew(() =>
                        {
                            foreach (Client client in clientList)
                            {
                                // Broadcast to all logged on users
                                var selfClient = clientList.FirstOrDefault(x => x.Key == receivedData.VCSessionKey);
                                if (client.Key != receivedData.VCSessionKey && client.isReady)
                                {
                                    if (selfClient != null)
                                    {
                                        volume = 1.0f - Math.Clamp(Vector3.Distance(client.Location, selfClient.Location) / 20, 0.0f, 1.0f);
                                        AudioPacket.VCVolume = volume;
                                        AudioPacket.VCSessionKey = selfClient.Key;
                                    }

                                    if (volume != 0.0f)
                                    {
                                        serverSocket.BeginSendTo(AudioPacket.GetPacketDataStream(), 0, AudioPacket.GetPacketDataStream().Length, SocketFlags.None, client.endPoint, new AsyncCallback(SendData), client.endPoint);
                                    }
                                }
                            }
                        });
                        break;

                    case PacketIdentifier.Ping:
                        var pingPacket = new Packet() { VCPacketDataIdentifier = PacketIdentifier.Ping };
                        serverSocket.BeginSendTo(pingPacket.GetPacketDataStream(), 0, pingPacket.GetPacketDataStream().Length, SocketFlags.None, epSender, new AsyncCallback(SendData), epSender);
                        clientList.FirstOrDefault(x => x.Key == receivedData.VCSessionKey).lastPing = DateTime.Now;
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[UDP] ReceiveData Error: " + ex.Message, "UDP Server");
                Console.ResetColor();
            }
        }

        public void SendData(IAsyncResult asyncResult)
        {
            try
            {
                serverSocket.EndSend(asyncResult);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[UDP] SendData Error: " + ex.Message, "UDP Server");
                Console.ResetColor();
            }
        }

        public string? CreateSessionKey(string PlayerId)
        {
            ClearTimeoutSessions();
            if (clientList.Exists(x => x.PlayerId == PlayerId) || SessionKeys.Exists(x => x.PlayerId == PlayerId))
                return null;

            var sessionKey = new SessionKey() { PlayerId = PlayerId, Key = GenerateKey() };

            SessionKeys.Add(sessionKey);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[HTTP] Created Session Key: Key: {sessionKey.Key} | Player Id: {PlayerId}");
            Console.ResetColor();

            return sessionKey.Key;
        }

        public void UpdateClientList(List<Player> players)
        {
            foreach(var client in clientList)
            {
                var player = players.FirstOrDefault(x => x.PlayerId == client.PlayerId);
                if(player != null)
                {
                    client.Location = player.Location;
                }
            }
        }

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

        private void ClearTimeoutSessions()
        {
            var kremoved = SessionKeys.RemoveAll(x => (DateTime.UtcNow - x.RegisteredAt).Seconds > 0);
            if (kremoved > 0) Console.WriteLine($"[HTTP] Removed Session Key(s): {kremoved} keys removed - Timeout.");

            var cremoved = clientList.RemoveAll(x => (DateTime.UtcNow - x.lastPing).Seconds > 10);
            if (cremoved > 0) Console.WriteLine($"[UDP] Removed Client(s): {cremoved} clients removed - Disconnect.");
        }
    }

    public class Client
    {
        public EndPoint endPoint { get; set; }
        public Vector3 Location { get; set; }
        public DateTime lastPing { get; set; } = DateTime.UtcNow;
        public string Key { get; set; } = "";
        public string PlayerId { get; set; } = "";
        public bool isReady { get; set; }
    }

    public class SessionKey
    {
        public string Key { get; set; } = "";
        public string PlayerId { get; set; } = "";
        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow.AddMinutes(5);
    }
}
