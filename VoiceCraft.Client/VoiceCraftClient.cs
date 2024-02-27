using NAudio.Wave;
using System.Threading.Tasks;
using VoiceCraft.Core.Packets;
using VoiceCraft.Core.Sockets;
using System;
using System.Collections.Generic;
using NAudio.Wave.SampleProviders;
using System.Net.Sockets;
using VoiceCraft.Core;
using System.Linq;

namespace VoiceCraft.Client
{
    public class VoiceCraftClient
    {
        public const string Version = "v1.0.3";

        //Audio Variables
        public WaveFormat AudioFormat { get; }
        public int FrameSizeMS { get; }
        public bool IsMuted { get; private set; }
        public bool IsDeafened { get; private set; }
        public bool LinearProximity { get; set; }
        public bool DirectionalHearing { get; set; }
        public MixingSampleProvider AudioOutput { get; }

        //Network Variables
        public ConnectionState ConnectionState { get; private set; }
        public PositioningTypes PositioningType { get; private set; }
        public Signalling Signalling { get; }
        public Voice Voice { get; }
        public string? IP { get; private set; }
        public ushort Key { get; private set; }

        //Participants/Channels
        public Dictionary<ushort, VoiceCraftParticipant> Participants { get; }
        public List<VoiceCraftChannel> Channels { get; }

        #region Events
        public delegate void SignallingConnected();
        public delegate void VoiceConnected();
        public delegate void ParticipantJoined(VoiceCraftParticipant participant, ushort key);
        public delegate void ParticipantLeft(VoiceCraftParticipant participant, ushort key);
        public delegate void ParticipantDeafenedStateChanged(VoiceCraftParticipant participant, bool value);
        public delegate void ParticipantMutedStateChanged(VoiceCraftParticipant participant, bool value);
        public delegate void ChannelAdded(VoiceCraftChannel channel);
        public delegate void ChannelJoined(VoiceCraftChannel channel);
        public delegate void ChannelLeft(VoiceCraftChannel channel);
        public delegate void Disconnected(string? reason = null);

        public event SignallingConnected? OnSignallingConnected;
        public event VoiceConnected? OnVoiceConnected;
        public event ParticipantJoined? OnParticipantJoined;
        public event ParticipantLeft? OnParticipantLeft;
        public event ParticipantDeafenedStateChanged? OnParticipantDeafenedStateChanged;
        public event ParticipantMutedStateChanged? OnParticipantMutedStateChanged;
        public event ChannelAdded? OnChannelAdded;
        public event ChannelJoined? OnChannelJoined;
        public event ChannelLeft? OnChannelLeft;
        public event Disconnected? OnDisconnected;
        #endregion

        public VoiceCraftClient(WaveFormat audioFormat, int frameSizeMS)
        {
            AudioFormat = audioFormat;
            FrameSizeMS = frameSizeMS;
            Signalling = new Signalling();
            Voice = new Voice();
            Participants = new Dictionary<ushort, VoiceCraftParticipant>();
            Channels = new List<VoiceCraftChannel>();
            AudioOutput = new MixingSampleProvider(audioFormat);
        }

        public async Task Connect(string iP, int port, ushort preferredKey, PositioningTypes positioningType)
        {
            if (ConnectionState != ConnectionState.Disconnected) throw new Exception("You must disconnect before connecting!");
            IP = iP;
            PositioningType = positioningType;

            //Event Registry
            Signalling.OnConnected += Signalling_Connected;
            Signalling.OnLogin += Signalling_Login;
            Signalling.OnLogout += Signalling_Logout;
            Signalling.OnDeafenUndeafen += Signalling_DeafenUndeafen;
            Signalling.OnMuteUnmute += Signalling_MuteUnmute;
            Signalling.OnAddChannel += Signalling_AddChannel;
            Signalling.OnJoinLeaveChannel += Signalling_JoinLeaveChannel;
            Signalling.OnDisconnected += Signalling_Disconnected;

            Voice.OnConnected += Voice_Connected;
            Voice.OnServerAudio += Voice_ServerAudio;
            Voice.OnDisconnected += Voice_Disconnected;

            ConnectionState = ConnectionState.Connecting;
            await Signalling.Connect(iP, port, preferredKey, positioningType, Version);
        }

        public void Disconnect(string? reason = null)
        {
            if (ConnectionState == ConnectionState.Disconnected) return; //Already Disconnected.

            //Event Registry
            Signalling.OnConnected -= Signalling_Connected;
            Signalling.OnLogin -= Signalling_Login;
            Signalling.OnLogout -= Signalling_Logout;
            Signalling.OnDeafenUndeafen -= Signalling_DeafenUndeafen;
            Signalling.OnMuteUnmute -= Signalling_MuteUnmute;
            Signalling.OnAddChannel -= Signalling_AddChannel;
            Signalling.OnJoinLeaveChannel -= Signalling_JoinLeaveChannel;
            Signalling.OnDisconnected -= Signalling_Disconnected;

            Voice.OnConnected -= Voice_Connected;
            Voice.OnDisconnected -= Voice_Disconnected;

            OnDisconnected?.Invoke(reason);
            Signalling.Disconnect();
            Voice.Disconnect();
            ConnectionState = ConnectionState.Disconnected;
        }

        #region Event Methods
        //Signalling
        private void Signalling_Connected(ushort port, ushort key = 0)
        {
            OnSignallingConnected?.Invoke();
            if (!string.IsNullOrWhiteSpace(IP))
            {
                Key = key;
                _ = Voice.Connect(IP, port, key);
            }
            else
            {
                Disconnect("IP WAS SOMEHOW EMPTY!");
            }
        }

        private void Signalling_Login(Core.Packets.Signalling.Login data, Socket socket)
        {
            if(!Participants.ContainsKey(data.Key))
            {
                var participant = new VoiceCraftParticipant(data.Name, AudioFormat, FrameSizeMS)
                {
                    IsDeafened = data.IsDeafened,
                    IsMuted = data.IsMuted
                };

                Participants.TryAdd(data.Key, participant);
                AudioOutput.AddMixerInput(participant.AudioOutput);
                OnParticipantJoined?.Invoke(participant, data.Key);
            }
        }

        private void Signalling_Logout(Core.Packets.Signalling.Logout data, Socket socket)
        {
            //Logout participant. If LoginKey is the same as this then disconnect.
            if (data.Key == Key)
            {
                Disconnect("Kicked or banned!");
            }
            else if(Participants.Remove(data.Key, out var participant))
            {
                participant.Dispose();
                OnParticipantLeft?.Invoke(participant, data.Key);
            }
        }

        private void Signalling_DeafenUndeafen(Core.Packets.Signalling.DeafenUndeafen data, Socket socket)
        {
            if(Participants.TryGetValue(data.Key, out var participant))
            {
                participant.IsDeafened = data.Value;
                OnParticipantDeafenedStateChanged?.Invoke(participant, data.Value);
            }
        }

        private void Signalling_MuteUnmute(Core.Packets.Signalling.MuteUnmute data, Socket socket)
        {
            if (Participants.TryGetValue(data.Key, out var participant))
            {
                participant.IsMuted = data.Value;
                OnParticipantDeafenedStateChanged?.Invoke(participant, data.Value);
            }
        }

        private void Signalling_AddChannel(Core.Packets.Signalling.AddChannel data, Socket socket)
        {
            var channel = new VoiceCraftChannel(data.Name)
            {
                RequiresPassword = data.RequiresPassword,
                ChannelId = data.ChannelId
            };
            Channels.Add(channel);
            OnChannelAdded?.Invoke(channel);
        }

        private void Signalling_JoinLeaveChannel(Core.Packets.Signalling.JoinLeaveChannel data, Socket socket)
        {
            var channel = Channels.FirstOrDefault(x => x.ChannelId == data.ChannelId);
            if(channel != null && channel.Joined != data.Joined)
            {
                channel.Joined = data.Joined;
                if(data.Joined)
                    OnChannelJoined?.Invoke(channel);
                else
                    OnChannelLeft?.Invoke(channel);
            }
        }

        private void Signalling_Disconnected(string? reason = null)
        {
            Disconnect(reason);
        }

        //Voice
        private void Voice_Connected()
        {
            ConnectionState = ConnectionState.Connected;
            OnVoiceConnected?.Invoke();
        }

        private void Voice_ServerAudio(Core.Packets.Voice.ServerAudio data, System.Net.EndPoint endPoint)
        {
            if(Participants.TryGetValue(data.Key, out var participant))
            {
                participant.ProximityVolume = LinearProximity ? (float)((Math.Exp(data.Volume) - 1) / (Math.E - 1)) : data.Volume;
                participant.EchoFactor = data.EchoFactor;
                participant.Muffled = data.Muffled;
                if (PositioningType != PositioningTypes.ClientSided && DirectionalHearing)
                {
                    participant.RightVolume = (float)Math.Max(0.5 + Math.Cos(data.Rotation) * 0.5, 0.2);
                    participant.LeftVolume = (float)Math.Max(0.5 - Math.Cos(data.Rotation) * 0.5, 0.2);
                }
                participant.AddSamples(data.Audio, data.PacketCount);
            }
        }

        private void Voice_Disconnected(string? reason = null)
        {
            Disconnect(reason);
        }
        #endregion
    }

    public enum ConnectionState
    {
        Connecting,
        Connected,
        Disconnected
    }
}
