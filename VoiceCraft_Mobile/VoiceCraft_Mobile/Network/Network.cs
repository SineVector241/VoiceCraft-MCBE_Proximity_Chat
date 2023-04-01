using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading.Tasks;
using VCSignalling_Packet;
using VCVoice_Packet;
using VoiceCraft_Mobile.Audio;
using VoiceCraft_Mobile.Models;
using Xamarin.Forms;

namespace VoiceCraft_Mobile.Network
{
    public class Network
    {
        public const string Version = "v1.3.0-alpha";
        public SignallingClient signallingClient { get; private set; }
        public VoiceClient voiceClient { get; private set; }

        //Users
        public List<ParticipantModel> participants { get; private set; }

        public Network()
        {
            signallingClient = new SignallingClient();
            voiceClient = new VoiceClient();
        }

        public void Disconnect()
        {
            try
            {
                signallingClient.Disconnect();
                voiceClient.Disconnect();
            }
            catch
            { }
        }

        public static Network Current { get; } = new Network();
    }

    public class SignallingClient
    {
        private UdpClient Client = new UdpClient();
        private bool isConnecting = false;
        private bool isConnected = false;
        private string localId = string.Empty;

        public string Key { get; private set; } = string.Empty;
        public string hostName { get; private set; } = string.Empty;
        public int VoicePort { get; private set; } = 0;

        //Heartbeat Variables
        private DateTime lastPing;
        private bool stopTimer = false;

        //Events
        public delegate void Connected(string key, string localServerId);
        public delegate void Disconnected(string reason);
        public delegate void Binded(string name);
        public delegate void AddParticipant(ParticipantModel participant);

        public event Connected OnConnect;
        public event Disconnected OnDisconnect;
        public event Binded OnBinded;
        public event AddParticipant OnParticipantLogin;

        public void Connect(string IP, int PORT, string Key = null, string localServerId = null)
        {
            localId = localServerId;
            hostName = IP;
            isConnecting = true;

            Client.Connect(IP, PORT);
            StartListeningAsync();
            lastPing = DateTime.UtcNow;
            stopTimer = false;

            var packet = new SignallingPacket() { PacketDataIdentifier = VCSignalling_Packet.PacketIdentifier.Login, PacketVersion = Network.Version, PacketLoginId = Key }.GetPacketDataStream();
            Client.Send(packet, packet.Length);

            Device.StartTimer(TimeSpan.FromSeconds(2), SendHeartbeatAsync);
        }

        public void Disconnect(string reason = null)
        {
            OnDisconnect?.Invoke(reason);

            Client.Close();
            Client.Dispose();
            Client = new UdpClient();

            Key = string.Empty;
            VoicePort = 0;
            isConnected = false;
            isConnecting = false;
            stopTimer = true;
        }

        public void Send(SignallingPacket packet)
        {
            if (!isConnected)
                return;

            var stream = packet.GetPacketDataStream();
            Client.Send(stream, stream.Length);
        }

        private async Task StartListeningAsync()
        {
            try
            {
                while (true)
                {
                    var data = await Client.ReceiveAsync();
                    var packet = new SignallingPacket(data.Buffer);
                    lastPing = DateTime.UtcNow;

                    //Handle Packet Here
                    switch (packet.PacketDataIdentifier)
                    {
                        case VCSignalling_Packet.PacketIdentifier.Accept:
                            Key = packet.PacketLoginId;
                            VoicePort = packet.PacketVoicePort;
                            isConnected = true;
                            isConnecting = false;
                            OnConnect?.Invoke(Key, localId);
                            break;

                        case VCSignalling_Packet.PacketIdentifier.Ping:
                            lastPing = DateTime.UtcNow;
                            break;

                        case VCSignalling_Packet.PacketIdentifier.Deny:
                            Disconnect("Server Denied Login Request. Possible LoginId Conflict");
                            break;

                        case VCSignalling_Packet.PacketIdentifier.Binded:
                            OnBinded?.Invoke(packet.PacketName);
                            break;

                        case VCSignalling_Packet.PacketIdentifier.Login:
                            OnParticipantLogin?.Invoke(new ParticipantModel() { 
                                LoginId = packet.PacketLoginId, 
                                Name = packet.PacketName,
                                WaveProvider = new NAudio.Wave.BufferedWaveProvider(AudioPlayback.Current.recordFormat)
                            });
                            break;
                    }
                }
            }
            catch (ObjectDisposedException)
            { //Ignore this exception. Usually means when the client is disposed this will throw. 
            }
            catch { }
            //Handle everything else but do nothing for the moment.
        }

        private bool SendHeartbeatAsync()
        {
            try
            {
                if (!isConnected && !isConnecting)
                    stopTimer = true;

                var packet = new SignallingPacket() { PacketDataIdentifier = VCSignalling_Packet.PacketIdentifier.Ping }.GetPacketDataStream();
                Client.Send(packet, packet.Length);

                if (DateTime.UtcNow.Subtract(lastPing).Seconds > 10)
                {
                    Disconnect("Connection Timed Out.");
                    stopTimer = true;
                }

                return !stopTimer;
            }
            catch
            {
                return !stopTimer;
            }
        }
    }


    public class VoiceClient
    {
        public UdpClient Client { get; private set; } = new UdpClient();
        
        private bool isConnected = false;
        private DateTime startedConnection = DateTime.UtcNow;

        //Events
        public delegate void Connected();
        public delegate void Disconnected(string reason);

        public event Connected OnConnect;
        public event Disconnected OnDisconnect;

        public void Connect(string IP, int PORT, string Key = null)
        {
            isConnected = false;
            startedConnection = DateTime.UtcNow;

            Client.Connect(IP, PORT);
            StartListeningAsync();

            var packet = new VoicePacket() { PacketDataIdentifier = VCVoice_Packet.PacketIdentifier.Login, PacketVersion = Network.Version, PacketLoginId = Key }.GetPacketDataStream();
            Client.Send(packet, packet.Length);

            Device.StartTimer(TimeSpan.FromSeconds(1), WaitForConnection);
        }

        public void Disconnect(string reason = null)
        {
            isConnected = false;

            OnDisconnect?.Invoke(reason);

            Client.Close();
            Client.Dispose();
            Client = new UdpClient();
        }

        public void Send(VoicePacket packet)
        {
            var stream = packet.GetPacketDataStream();
            Client.Send(stream, stream.Length);
        }

        private async Task StartListeningAsync()
        {
            try
            {
                while (true)
                {
                    var data = await Client.ReceiveAsync();
                    var packet = new VoicePacket(data.Buffer);

                    //Handle Packet Here
                    switch (packet.PacketDataIdentifier)
                    {
                        case VCVoice_Packet.PacketIdentifier.Accept:
                            OnConnect?.Invoke();
                            isConnected = true;
                            break;

                        case VCVoice_Packet.PacketIdentifier.Deny:
                            Disconnect("Voice Server Denied Login Request.");
                            break;

                        case VCVoice_Packet.PacketIdentifier.Audio:
                            Console.WriteLine("Received Audio");
                            break;
                    }
                }
            }
            catch (ObjectDisposedException)
            { //Ignore this exception. Usually means when the client is disposed this will throw. 
            }
            catch { }
            //Handle everything else but do nothing for the moment.
        }

        private bool WaitForConnection()
        {
            if (isConnected)
            {
                OnConnect?.Invoke();
                return false;
            }
            if (DateTime.UtcNow.Subtract(startedConnection).Seconds > 5)
            {
                Disconnect("Voice Connection Timed Out.");
                return false;
            }
            return true;
        }
    }
}
