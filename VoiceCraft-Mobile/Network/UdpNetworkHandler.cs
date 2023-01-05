using VoiceCraft_Mobile.Utils;
using Android.Media;
using VoiceCraft_Mobile.Repositories;

namespace VoiceCraft_Mobile.Network
{
    public class UdpNetworkHandler
    {
        public DateTime _LastPing { get; set; } = DateTime.UtcNow;
        public bool _IsLoggedIn { get; set; }
        public Timer? _Pinger { get; set; } = null;
        public string? _Key { get; set; } = null;
        
        //Events
        public event EventHandler Logout;
        public event EventHandler<ClientLoginEventArgs> ClientLogin;
        public event EventHandler<ClientLogoutEventArgs> ClientLogout;
        public event EventHandler<AudioReceivedEventArgs> AudioReceived;

        //Event Methods
        protected virtual void OnLogout()
        {
            EventHandler handler = Logout;
            if(handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        protected virtual void OnClientLogin(ClientLoginEventArgs e)
        {
            EventHandler<ClientLoginEventArgs> handler = ClientLogin;
            if(handler != null)
            {
                handler(this, e);
            }
        }

        protected virtual void OnClientLogout(ClientLogoutEventArgs e)
        {
            EventHandler<ClientLogoutEventArgs> handler = ClientLogout;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected virtual void OnAudioReceived(AudioReceivedEventArgs e)
        {
            EventHandler<AudioReceivedEventArgs> handler = AudioReceived;
            if(handler != null)
            {
                handler(this, e);
            }
        }

        //Methods
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
                        OnAudioReceived(new AudioReceivedEventArgs() { Audio = G722ChatCodec.CodecInstance.Decode(packet.VCAudioBuffer, 0, packet.VCAudioBuffer.Length), Key = packet.VCSessionKey, Volume = packet.VCVolume });
                        Task.Factory.StartNew(() => {
                            AudioPlayback.Instance.audioMixer.AddSamples(G722ChatCodec.CodecInstance.Decode(packet.VCAudioBuffer, 0, packet.VCAudioBuffer.Length));
                        });
                    }
                    catch { }
                    break;

                case PacketIdentifier.Login:
                    OnClientLogin(new ClientLoginEventArgs() { Username = packet.VCName, SessionKey = packet.VCSessionKey });
                    break;

                case PacketIdentifier.Logout:
                    OnClientLogout(new ClientLogoutEventArgs() { SessionKey = packet.VCSessionKey });
                    break;
            }
        }

        public void Ready()
        {
            if (_IsLoggedIn)
            {
                UdpNetwork.Instance.SendPacket(new Packet() { VCPacketDataIdentifier = PacketIdentifier.Ready, VCSessionKey = _Key });
            }
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

    public class ClientLoginEventArgs : EventArgs
    {
        public string Username { get; set; } = "";
        public string SessionKey { get; set; } = "";
    }

    public class ClientLogoutEventArgs : EventArgs
    {
        public string SessionKey { get; set; } = "";
    }

    public class AudioReceivedEventArgs : EventArgs
    {
        public string Key { get; set; } = "";
        public float Volume { get; set; } = 0f;
        public byte[] Audio { get; set; } = new byte[1600];
    }
}
