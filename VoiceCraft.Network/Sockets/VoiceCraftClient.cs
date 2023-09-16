using Concentus.Structs;
using NAudio.Wave.SampleProviders;
using NAudio.Wave;
using System.Threading;
using VoiceCraft.Core.Packets;
using VoiceCraft.Core.Sockets.Client;
using System.Collections.Concurrent;
using System;

namespace VoiceCraft.Core.Sockets
{
    public class VoiceCraftClient
    {
        //Constants
        public const int SampleRate = 48000;

        //Variables
        public CancellationTokenSource CTS { get; }
        public string IP { get; private set; } = string.Empty;
        public int Port { get; private set; }
        public ushort LoginKey { get; private set; }
        public string Version { get; private set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public PositioningTypes PositioningType { get; private set; }

        //Changeable Variables
        public bool DirectionalHearing { get; set; }
        public bool LinearVolume { get; set; }

        //Server Data
        public ConcurrentDictionary<ushort, ClientParticipant> Participants { get; private set; } = new ConcurrentDictionary<ushort, ClientParticipant>();
        public uint PacketCount { get; private set; }

        //Audio Variables
        public int RecordLengthMS { get; }
        public WaveFormat RecordFormat { get; } = new WaveFormat(SampleRate, 1);
        public WaveFormat PlaybackFormat { get; } = WaveFormat.CreateIeeeFloatWaveFormat(SampleRate, 2);
        public MixingSampleProvider Mixer { get; }

        //Codec Encoder
        private readonly OpusEncoder Encoder;

        //Sockets
        public SignallingSocket Signalling { get; }
        public VoiceSocket Voice { get; }

        //Delegates
        public delegate void Connected();
        public delegate void Binded(string? name);
        public delegate void ParticipantJoined(ClientParticipant participant);
        public delegate void ParticipantLeft(ClientParticipant participant);
        public delegate void ParticipantUpdated(ClientParticipant participant);
        public delegate void Disconnected(string? reason);

        //Events
        public event Connected? OnConnected;
        public event Binded? OnBinded;
        public event ParticipantJoined? OnParticipantJoined;
        public event ParticipantLeft? OnParticipantLeft;
        public event ParticipantUpdated? OnParticipantUpdated;
        public event Disconnected? OnDisconnected;

        public VoiceCraftClient(ushort LoginKey, PositioningTypes PositioningType, string Version, int RecordLengthMS = 40)
        {
            //Setup variables
            this.LoginKey = LoginKey;
            this.Version = Version;
            this.PositioningType = PositioningType;
            this.RecordLengthMS = RecordLengthMS;

            Encoder = new OpusEncoder(SampleRate, 1, Concentus.Enums.OpusApplication.OPUS_APPLICATION_VOIP)
            {
                Bitrate = 64000,
                Complexity = 5,
                UseVBR = true,
                PacketLossPercent = 50,
                UseInbandFEC = true
            };

            Mixer = new MixingSampleProvider(PlaybackFormat) { ReadFully = true };

            //Socket Setup
            CTS = new CancellationTokenSource();
            Signalling = new SignallingSocket(CTS.Token);
            Voice = new VoiceSocket(CTS.Token);

            //Event Registration in login order.
            Signalling.OnAcceptPacketReceived += SignallingAccept;
            Signalling.OnDenyPacketReceived += SignallingDeny;
            Voice.OnAcceptPacketReceived += VoiceAccept;
            Voice.OnDenyPacketReceived += VoiceDeny;
            Signalling.OnBindedPacketReceived += SignallingBinded;
            Signalling.OnLoginPacketReceived += SignallingLogin;
            Voice.OnServerAudioPacketReceived += VoiceServerAudio;
            Signalling.OnLogoutPacketReceived += SignallingLogout;

            Signalling.OnDeafenPacketReceived += SignallingDeafen;
            Signalling.OnUndeafenPacketReceived += SignallingUndeafen;
            Signalling.OnMutePacketReceived += SignallingMute;
            Signalling.OnUnmutePacketReceived += SignallingUnmute;
        }

        //Signalling Event Methods
        private void SignallingAccept(Packets.Signalling.Accept packet)
        {
            Voice.Connect(IP, packet.VoicePort);
            Voice.SendPacketAsync(new VoicePacket() { PacketType = VoicePacketTypes.Login, PacketData = new Packets.Voice.Login() });
            //Packets could be dropped. So we need to loop. This will be implemented later.
        }

        private void SignallingDeny(Packets.Signalling.Deny packet)
        {
            CTS.Cancel();
            OnDisconnected?.Invoke(packet.Reason);
        }

        private void SignallingBinded(Packets.Signalling.Binded packet)
        {
            OnBinded?.Invoke(packet.Name);
        }

        private void SignallingLogin(Packets.Signalling.Login packet)
        {
            //Add participant to list
            if(!Participants.ContainsKey(packet.LoginKey))
            {
                var participant = new ClientParticipant(packet.Name, PlaybackFormat, RecordLengthMS);
                Participants.TryAdd(packet.LoginKey, participant);
                OnParticipantJoined?.Invoke(participant);
            }
        }

        private void SignallingLogout(Packets.Signalling.Logout packet)
        {
            //Logout participant. If LoginKey is the same as this then disconnect.
            if (packet.LoginKey == LoginKey)
            {
                CTS.Cancel();
                OnDisconnected?.Invoke("Kicked or banned.");
            }
            else
            {
                Participants.TryRemove(packet.LoginKey, out var participant);
                if(participant != null)
                {
                    OnParticipantJoined?.Invoke(participant);
                }
            }
        }

        private void SignallingDeafen(Packets.Signalling.Deafen packet)
        {
            Participants.TryGetValue(packet.LoginKey, out var participant);
            if(participant != null)
            {
                participant.Deafened = true;
            }
        }

        private void SignallingUndeafen(Packets.Signalling.Undeafen packet)
        {
            Participants.TryGetValue(packet.LoginKey, out var participant);
            if (participant != null)
            {
                participant.Deafened = false;
            }
        }

        private void SignallingMute(Packets.Signalling.Mute packet)
        {
            Participants.TryGetValue(packet.LoginKey, out var participant);
            if (participant != null)
            {
                participant.Muted = true;
            }
        }

        private void SignallingUnmute(Packets.Signalling.Unmute packet)
        {
            Participants.TryGetValue(packet.LoginKey, out var participant);
            if (participant != null)
            {
                participant.Muted = false;
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
            Participants.TryGetValue(packet.LoginKey, out var participant);
            if (participant != null)
            {
                participant.ProximityVolume = LinearVolume ? (float)((Math.Exp(packet.Volume) - 1) / (Math.E - 1)) : packet.Volume;
                participant.EchoProvider.EchoFactor = packet.EchoFactor;
                //Directional Hearing implemented later...
                participant.AddAudioSamples(packet.Audio, packet.PacketCount);
            }
        }

        //Public Methods
        public void Connect(string IP, int Port)
        {
            this.IP = IP;
            this.Port = Port;
            Signalling.Connect(IP, Port);
            Signalling.SendPacketAsync(new SignallingPacket() { 
                PacketType = SignallingPacketTypes.Login,
                PacketData = new Packets.Signalling.Login() { 
                    LoginKey = LoginKey,
                    PositioningType = PositioningType,
                    Version = Version
                } 
            });
        }
    }
}
