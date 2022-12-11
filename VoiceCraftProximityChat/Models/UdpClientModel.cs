using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using VoiceCraftProximityChat.Utils;

namespace VoiceCraftProximityChat.Models
{
    public class UdpClientModel : IUdpClient
    {
        private static IPEndPoint endPoint;
        public static string _Key { get; set; }
        public static bool IsConnected { get; private set; }
        public static float VolumeGain { get; set; } = 0.0f;
        private static UdpClient client { get; set; } = new UdpClient();
        private static DateTime LastPing { get; set; } = DateTime.UtcNow;
        private static Timer? pingChecker { get; set; } = null;

        public void Connect(IPAddress IPAddress, int Port)
        {
            endPoint = new IPEndPoint(IPAddress, Port);
            client.Connect(endPoint);
            client.BeginReceive(new AsyncCallback(RecievePacket), null);
        }

        public void Login(string Key, Action<bool> func)
        {
            Task.Run(() =>
            {
                _Key = Key;
                SendPacket(new Packet() { VCSessionKey = _Key, VCPacketDataIdentifier = PacketIdentifier.Login });
                var TimePoint = DateTime.UtcNow;
                while (!IsConnected && (DateTime.UtcNow - TimePoint).Seconds < 5)
                {}
                LastPing = DateTime.UtcNow;
                func(IsConnected);
            });
        }

        public void SendPacket(Packet packet)
        {
            client.Send(packet.GetPacketDataStream());
        }

        public void RecievePacket(IAsyncResult asyncResult)
        {
            try
            {
                byte[] received = client.EndReceive(asyncResult, ref endPoint);
                HandlePacket(new Packet(received));
                client.BeginReceive(new AsyncCallback(RecievePacket), null);
            }
            catch { }
        }

        public void HandlePacket(Packet packetData)
        {
            switch(packetData.VCPacketDataIdentifier)
            {
                case PacketIdentifier.Accept:
                    IsConnected = true;
                    pingChecker = new Timer(PingCheck, null, 0, 2000);
                    break;

                case PacketIdentifier.Ping:
                    LastPing = DateTime.UtcNow;
                    break;

                case PacketIdentifier.AudioStream:
                    try
                    {
                        
                        AudioPlaybackModel.Instance.PlaySound(packetData.VCAudioBuffer, packetData.VCVolume + VolumeGain, packetData.VCSessionKey);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                    break;
            }
        }

        public void Dispose()
        {
            client.Close();
            client.Dispose();
            if (pingChecker != null)
                pingChecker.Dispose();
            client = new UdpClient();
        }

        private static void PingCheck(object? state)
        {
            try
            {
                if ((DateTime.UtcNow - LastPing).Seconds > 10)
                    IsConnected = false;

                Packet packet = new Packet() { VCPacketDataIdentifier = PacketIdentifier.Ping, VCSessionKey = _Key };
                client.Send(packet.GetPacketDataStream());
            }
            catch
            {
            }
        }
    }
}
