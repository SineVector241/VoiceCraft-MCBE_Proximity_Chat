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

        public UdpServer(int Port)
        {
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
