using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VoiceCraftProximityChat.Repositories;
using VoiceCraftProximityChat.Utils;

namespace VoiceCraftProximityChat.Network
{
    public class UdpNetworkHandler
    {
        public DateTime _LastPing { get; set; } = DateTime.UtcNow;
        public bool _IsLoggedIn { get; set; }
        public Timer? _Pinger { get; set; } = null;
        public string? _Key { get; set; } = null;
        public event EventHandler Logout;

        protected virtual void OnLogout()
        {
            EventHandler handler = Logout;
            if(handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        public void HandlePacket(Packet packet)
        {
            switch (packet.VCPacketDataIdentifier)
            {
                case PacketIdentifier.Accept:
                    _IsLoggedIn = true;
                    _LastPing = DateTime.UtcNow;
                    _Pinger = new Timer(Pinger, null, 0, 2000);
                    break;

                case PacketIdentifier.Ping:
                    _LastPing = DateTime.UtcNow;
                    break;

                case PacketIdentifier.AudioStream:
                    try
                    {
                        AudioPlayback.Instance.PlaySound(packet.VCAudioBuffer, packet.VCVolume, packet.VCSessionKey);
                    }
                    catch { }
                    break;
            }
        }

        public void Ready()
        {
            if (_IsLoggedIn)
                UdpNetwork.Instance.SendPacket(new Packet() { VCPacketDataIdentifier = PacketIdentifier.Ready, VCSessionKey = _Key });
        }

        public void Disconnect()
        {
            _IsLoggedIn = false;
            UdpNetwork.Instance.DisposeConnection();
        }

        private void Pinger(object? state)
        {
            try
            {
                if ((DateTime.UtcNow - _LastPing).Seconds > 10 && _IsLoggedIn)
                {
                    _IsLoggedIn = false;
                    UdpNetwork.Instance.DisposeConnection();
                    OnLogout();
                }
                else
                {
                    UdpNetwork.Instance.SendPacket(new Packet() { VCPacketDataIdentifier = PacketIdentifier.Ping, VCSessionKey = _Key });
                }
            }
            catch { }
        }

        public static UdpNetworkHandler Instance { get; } = new UdpNetworkHandler();
    }
}
