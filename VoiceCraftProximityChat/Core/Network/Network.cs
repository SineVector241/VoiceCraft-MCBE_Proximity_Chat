using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace VoiceCraftProximityChat.Core.Network
{
    public static class Network
    {
        public static string KEY = "";
        private static bool isConnected = false;
        private static IPEndPoint endPoint;
        public static UdpClient client = new UdpClient();
        public static DateTime lastPing = DateTime.UtcNow;
        public static Timer timer;

        private static void recv(IAsyncResult res)
        {
            try
            {
                byte[] received = client.EndReceive(res, ref endPoint);
                client.BeginReceive(new AsyncCallback(recv), null);
                Packet packetData = new Packet(received);
                switch (packetData.VCPacketDataIdentifier)
                {
                    case PacketIdentifier.Accept:
                        isConnected = true;
                        break;

                    case PacketIdentifier.AudioStream:
                        isConnected = true;
                        Task.Run(() =>
                        {
                            Audio.AddSamples(received, packetData.VCVolume);
                        });
                        break;

                    case PacketIdentifier.Ping:
                        lastPing = DateTime.UtcNow;
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                if(isConnected)
                    Audio.waveIn.StopRecording();
                client.Dispose();
                client = new UdpClient();
                isConnected = false;
            }
        }

        public static bool Login(string _IP, int _PORT, string _KEY)
        {
            try
            {
                endPoint = new IPEndPoint(IPAddress.Parse(_IP), _PORT);
                KEY = _KEY;
                client.Connect(endPoint);
                client.BeginReceive(new AsyncCallback(recv), null);

                Packet packet = new Packet() { VCPacketDataIdentifier = PacketIdentifier.Login, VCSessionKey = KEY };
                client.Send(packet.GetPacketDataStream());
                var time = DateTime.UtcNow;
                while ((DateTime.UtcNow - time).Seconds < 5 && !isConnected)
                { }
                lastPing = DateTime.UtcNow;
                return isConnected;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
            return false;
        }

        public static void SendReadyEvent()
        {
            Packet packet = new Packet() { VCPacketDataIdentifier = PacketIdentifier.Ready, VCSessionKey = KEY };
            client.Send(packet.GetPacketDataStream());
        }

        public static void Shutdown()
        {
            MessageBox.Show("Lost connection with server: Timed Out. Shutting down application...");
            Environment.Exit(0);
        }

        public static void Dispose()
        {
            client.Dispose();
            client = new UdpClient();
        }

        public static void lastPingCheck(object? state)
        {
            try
            {
                if((DateTime.Now - lastPing).Seconds > 5)
                {
                    timer.Dispose();
                    Shutdown();
                }
                Packet packet = new Packet() { VCPacketDataIdentifier = PacketIdentifier.Ping, VCSessionKey = KEY };
                client.Send(packet.GetPacketDataStream());
            }
            catch
            {
            }
        }
    }
}