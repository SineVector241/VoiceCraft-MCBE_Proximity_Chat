using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using VoiceCraft.Core.Packets;
using VoiceCraft.Core.Sockets.Client;

namespace VoiceCraft.Core.Sockets
{
    public class VoiceCraftClient
    {
        public CancellationTokenSource CTS { get; }
        public SignallingSocket Signalling { get; }
        public VoiceSocket Voice { get; }
        public string IP { get; private set; } = string.Empty;
        public int Port { get; private set; }
        public ushort LoginKey { get; private set; }
        public string Version { get; private set; } = string.Empty;
        public PositioningTypes PositioningType { get; private set; }

        //Delegates
        public delegate void Connected();
        public delegate void Disconnected(string? reason);

        //Events
        public event Connected? OnConnected;
        public event Disconnected? OnDisconnected;

        public VoiceCraftClient(ushort LoginKey, PositioningTypes PositioningType, string Version)
        {
            this.LoginKey = LoginKey;
            this.Version = Version;
            this.PositioningType = PositioningType;

            CTS = new CancellationTokenSource();
            Signalling = new SignallingSocket(CTS.Token);
            Voice = new VoiceSocket(CTS.Token);

            //Event Registration in login order.
            Signalling.OnAcceptPacketReceived += SignallingAccept;
            Signalling.OnDenyPacketReceived += SignallingDeny;
            Voice.OnAcceptPacketReceived += VoiceAccept;
            Voice.OnDenyPacketReceived += VoiceDeny;
            Signalling.OnLoginPacketReceived += SignallingLogin;
            Voice.OnServerAudioPacketReceived += VoiceServerAudio;
            Signalling.OnLogoutPacketReceived += SignallingLogut;
        }

        //Signalling Event Methods
        private void SignallingAccept(Packets.Signalling.Accept packet)
        {
            Voice.Connect(IP, packet.VoicePort);
            Voice.SendPacketAsync(new VoicePacket() { PacketType = VoicePacketTypes.Login, PacketData = new Packets.Voice.Login() });
        }

        private void SignallingDeny(Packets.Signalling.Deny packet)
        {
            CTS.Cancel();
            OnDisconnected?.Invoke(packet.Reason);
        }

        private void SignallingLogin(Packets.Signalling.Login packet)
        {
            //Add participant to list
        }

        private void SignallingLogut(Packets.Signalling.Logout packet)
        {
            //Logout participant. If LoginKey is the same as this then disconnect.
            if (packet.LoginKey == LoginKey)
            {
                CTS.Cancel();
                OnDisconnected?.Invoke("Kicked or banned.");
            }
        }

        //Voice Event Methods
        private void VoiceAccept(Packets.Voice.Accept packet)
        {
            OnConnected?.Invoke();
        }

        private void VoiceDeny(Packets.Voice.Deny packet)
        {
            CTS.Cancel();
            OnDisconnected?.Invoke(packet.Reason);
        }

        private void VoiceServerAudio(Packets.Voice.ServerAudio packet)
        {
            //Add audio to a participant
        }

        //Public Methods
        public void Connect(string IP, int Port)
        {
            this.IP = IP;
            this.Port = Port;
            Signalling.Connect(IP, Port);
            Signalling.SendPacketAsync(new SignallingPacket() { PacketType = SignallingPacketTypes.Login, PacketData = new Packets.Signalling.Login() { LoginKey = LoginKey, PositioningType = PositioningType, Version = Version} });
        }
    }
}
