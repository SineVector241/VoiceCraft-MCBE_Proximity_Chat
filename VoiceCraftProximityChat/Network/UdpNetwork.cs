using System;
using System.Net;
using System.Net.Sockets;
using VoiceCraftProximityChat.Utils;

namespace VoiceCraftProximityChat.Network
{
    public class UdpNetwork
    {
        public UdpClient? _UdpClient { get; set; }
        private IPEndPoint? _IPEndPoint { get; set; }

        public UdpNetwork()
        {
            _UdpClient = new UdpClient();
        }

        public void Connect(IPAddress iPAddress, int Port, string Key)
        {
            try
            {
                if (_UdpClient.Client.Connected)
                    DisposeConnection();
                _IPEndPoint = new IPEndPoint(iPAddress, Port);
                _UdpClient.Connect(_IPEndPoint);
                _UdpClient.BeginReceive(new AsyncCallback(Listen), null);
                UdpNetworkHandler.Instance._Key = Key;
            }
            catch { }
        }

        public void Authenticate(Action<bool> LoginAction)
        {
            var packet = new Packet() { VCPacketDataIdentifier = PacketIdentifier.Login, VCSessionKey = UdpNetworkHandler.Instance._Key };
            SendPacket(packet);

            var TimePoint = DateTime.UtcNow;
            while (!UdpNetworkHandler.Instance._IsLoggedIn && (DateTime.UtcNow - TimePoint).Seconds < 5)
            {
            }
            LoginAction(UdpNetworkHandler.Instance._IsLoggedIn);
        }

        public void Listen(IAsyncResult asyncResult)
        {
            try
            {
                var ep = _IPEndPoint;
                byte[] received = _UdpClient.EndReceive(asyncResult, ref ep);
                _UdpClient.BeginReceive(new AsyncCallback(Listen), null);

                var packet = new Packet(received);
                UdpNetworkHandler.Instance.HandlePacket(packet);
            }
            catch
            { }
        }

        public void SendPacket(Packet packet)
        {
            if (_UdpClient.Client.Connected)
                _UdpClient.Send(packet.GetPacketDataStream());
        }

        public void DisposeConnection()
        {
            _UdpClient?.Dispose();
            _UdpClient = new UdpClient();
        }

        public static UdpNetwork Instance { get; } = new UdpNetwork();
    }
}
