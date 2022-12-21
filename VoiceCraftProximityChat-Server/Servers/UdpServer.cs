using System.Net;
using System.Net.Sockets;
using System.Numerics;
using VoiceCraftProximityChat_Server.Dependencies;

namespace VoiceCraftProximityChat_Server.Servers
{
    public class UdpServer
    {
        private Socket serverSocket;
        private Packet packets;
        private byte[] dataStream = new byte[4824];
        private EndPoint endPoint;

        public UdpServer(int Port)
        {
            ServerData.Data.ClientConnect += ClientConnect;
            ServerData.Data.ClientDisconnect += ClientDisconnect;

            packets = new Packet();
            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPEndPoint serverEp = new IPEndPoint(IPAddress.Any, Port);
            serverSocket.Bind(serverEp);
            IPEndPoint clients = new IPEndPoint(IPAddress.Any, 0);
            endPoint = clients;
            serverSocket.BeginReceiveFrom(dataStream, 0, dataStream.Length, SocketFlags.None, ref endPoint, new AsyncCallback(ReceiveData), endPoint);

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"Started UDP Server: Port - {Port}");
            Console.ResetColor();
        }

        //Event Handlers
        private void ClientDisconnect(object? sender, ClientDisconnectEventArgs e)
        {
            Console.WriteLine($"[UDP] Client Disconnected: Key: {e.SessionKey} | Username: {e.Username}");
            var LogoutPacket = new Packet() { VCPacketDataIdentifier = PacketIdentifier.Logout, VCSessionKey = e.SessionKey }.GetPacketDataStream();
            Task.Factory.StartNew(() =>
            {
                var clientList = ServerData.Data.ClientList;
                foreach (Client client in clientList)
                {
                    // Broadcast to all logged on users
                    if (client.Key != e.SessionKey && client.isReady)
                    {
                        serverSocket.BeginSendTo(LogoutPacket, 0, LogoutPacket.Length, SocketFlags.None, client.endPoint, new AsyncCallback(SendData), client.endPoint);
                    }
                }
            });
        }

        private void ClientConnect(object? sender, ClientConnectEventArgs e)
        {
            Console.WriteLine($"[UDP] Client Connected: Key: {e.SessionKey} | Username: {e.Username}");
            var LoginPacket = new Packet() { VCPacketDataIdentifier = PacketIdentifier.Login, VCSessionKey = e.SessionKey, VCName = e.Username }.GetPacketDataStream();

            //Broadcast Login to all other clients
            Task.Factory.StartNew(() =>
            {
                var clientList = ServerData.Data.ClientList;
                foreach (Client client in clientList)
                {
                    // Broadcast to all logged on users
                    if (client.Key != e.SessionKey && client.isReady)
                    {
                        serverSocket.BeginSendTo(LoginPacket, 0, LoginPacket.Length, SocketFlags.None, client.endPoint, new AsyncCallback(SendData), client.endPoint);
                    }
                }
            });
        }

        //Core Methods
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

                switch (receivedData.VCPacketDataIdentifier)
                {
                    case PacketIdentifier.Login:
                        Console.WriteLine($"[UDP] Recieved new login request: Key: {receivedData.VCSessionKey} | Address: {epSender}");

                        if (ServerData.Data.ClientLogin(receivedData.VCSessionKey, epSender))
                        {
                            var packet = new Packet() { VCPacketDataIdentifier = PacketIdentifier.Accept }.GetPacketDataStream();
                            serverSocket.BeginSendTo(packet, 0, packet.Length, SocketFlags.None, epSender, new AsyncCallback(SendData), epSender);
                        }
                        else
                        {
                            var packet = new Packet() { VCPacketDataIdentifier = PacketIdentifier.Deny }.GetPacketDataStream();
                            serverSocket.BeginSendTo(packet, 0, packet.Length, SocketFlags.None, epSender, new AsyncCallback(SendData), epSender);
                        }
                        break;

                    case PacketIdentifier.Ready:
                        try
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine($"[UDP] Recieved ReadyEvent: Key: {receivedData.VCSessionKey} | Address: {epSender}");
                            Console.ResetColor();

                            var client = ServerData.Data.GetClientBySessionKey(receivedData.VCSessionKey);
                            client.isReady = true;
                            ServerData.Data.UpdateClient(client);

                            //Send client list to the client that just connected.
                            Task.Factory.StartNew(() =>
                            {
                                var clientList = ServerData.Data.ClientList;
                                foreach (Client client in clientList)
                                {
                                    var InfoPacket = new Packet() { VCPacketDataIdentifier = PacketIdentifier.Login, VCSessionKey = client.Key, VCName = client.Username }.GetPacketDataStream();
                                    if (client.Key != receivedData.VCSessionKey)
                                    {
                                        serverSocket.BeginSendTo(InfoPacket, 0, InfoPacket.Length, SocketFlags.None, epSender, new AsyncCallback(SendData), epSender);
                                    }
                                }
                            });
                        }
                        catch
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"[UDP] Error: Could not update client: {receivedData.VCSessionKey}");
                            Console.ResetColor();
                        }
                        break;

                    case PacketIdentifier.AudioStream:
                        var AudioPacket = new Packet() { VCPacketDataIdentifier = PacketIdentifier.AudioStream, VCAudioBuffer = receivedData.VCAudioBuffer };
                        var volume = 0.0f;
                        var clientList = ServerData.Data.ClientList;
                        Task.Factory.StartNew(() =>
                        {
                            foreach (Client client in clientList)
                            {
                                // Broadcast to all logged on users
                                var selfClient = clientList.FirstOrDefault(x => x.Key == receivedData.VCSessionKey);
                                if (client.Key != receivedData.VCSessionKey && client.isReady && client.EnviromentId == selfClient.EnviromentId)
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
                        try
                        {
                            var pingPacket = new Packet() { VCPacketDataIdentifier = PacketIdentifier.Ping };
                            serverSocket.BeginSendTo(pingPacket.GetPacketDataStream(), 0, pingPacket.GetPacketDataStream().Length, SocketFlags.None, epSender, new AsyncCallback(SendData), epSender);
                            var pingClient = ServerData.Data.GetClientBySessionKey(receivedData.VCSessionKey);
                            pingClient.lastPing = DateTime.UtcNow;
                            ServerData.Data.UpdateClient(pingClient);
                        }
                        catch
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"[UDP] Error: Could not update client: {receivedData.VCSessionKey}");
                            Console.ResetColor();
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[UDP] ReceiveData Error: " + ex.Message, "UDP Server");
                Console.ResetColor();
            }
            finally
            {
                serverSocket.BeginReceiveFrom(dataStream, 0, dataStream.Length, SocketFlags.None, ref endPoint, new AsyncCallback(ReceiveData), endPoint);
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
    }
}
