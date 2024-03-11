using NAudio.Wave;
using System.Threading.Tasks;
using VoiceCraft.Core.Sockets;
using System;
using System.Collections.Generic;
using NAudio.Wave.SampleProviders;
using System.Net.Sockets;
using System.Linq;
using OpusSharp;
using VoiceCraft.Core;
using VoiceCraft.Core.Packets;
using System.Net;

namespace VoiceCraft.Client
{
    public class VoiceCraftClient : IDisposable
    {
        public const string Version = "v1.0.3";
        public bool IsDisposed { get; private set; }

        //Audio Variables
        public WaveFormat AudioFormat { get; }
        public WaveFormat PlaybackFormat { get; }
        public int FrameSizeMS { get; }
        public bool IsMuted { get; private set; }
        public bool IsDeafened { get; private set; }
        public bool LinearProximity { get; set; }
        public bool DirectionalHearing { get; set; }
        public MixingSampleProvider AudioOutput { get; }
        public uint PacketCount { get; private set; }

        //Network Variables
        public ConnectionState ConnectionState { get; private set; } = ConnectionState.Disconnected;
        public PositioningTypes PositioningType { get; private set; }
        public Signalling Signalling { get; }
        public Voice Voice { get; }
        public MCWSS MCWSS { get; private set; }
        public string? IP { get; private set; }
        public string? Name { get; private set; }
        public ushort PublicId { get; private set; }
        public int PrivateId { get; private set; }

        //Participants/Channels
        public Dictionary<ushort, VoiceCraftParticipant> Participants { get; }
        public List<VoiceCraftChannel> Channels { get; }

        //Encoder
        private OpusEncoder Encoder { get; set; }

        #region Events
        public delegate void SignallingConnected();
        public delegate void VoiceConnected();
        public delegate void Binded(string? name);
        public delegate void Unbinded();
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
        public event Binded? OnBinded;
        public event Unbinded? OnUnbinded;
        public event ParticipantJoined? OnParticipantJoined;
        public event ParticipantLeft? OnParticipantLeft;
        public event ParticipantDeafenedStateChanged? OnParticipantDeafenedStateChanged;
        public event ParticipantMutedStateChanged? OnParticipantMutedStateChanged;
        public event ChannelAdded? OnChannelAdded;
        public event ChannelJoined? OnChannelJoined;
        public event ChannelLeft? OnChannelLeft;
        public event Disconnected? OnDisconnected;
        #endregion

        public VoiceCraftClient(WaveFormat audioFormat, int frameSizeMS = 20, int MCWSSPort = 8080)
        {
            AudioFormat = audioFormat;
            PlaybackFormat = WaveFormat.CreateIeeeFloatWaveFormat(AudioFormat.SampleRate, 2);
            FrameSizeMS = frameSizeMS;
            Signalling = new Signalling();
            Voice = new Voice();
            MCWSS = new MCWSS(MCWSSPort);
            Participants = new Dictionary<ushort, VoiceCraftParticipant>();
            Channels = new List<VoiceCraftChannel>();

            Encoder = new OpusEncoder(AudioFormat.SampleRate, AudioFormat.Channels, OpusSharp.Enums.Application.VOIP);
            Encoder.Bitrate = 32000;
            Encoder.PacketLossPerc = 50;
            AudioOutput = new MixingSampleProvider(PlaybackFormat) { ReadFully = true };
        }

        public async Task Connect(string iP, int port, ushort preferredKey, PositioningTypes positioningType)
        {
            try
            {
                if (ConnectionState != ConnectionState.Disconnected) throw new Exception("You must disconnect before connecting!");
                IP = iP;
                PositioningType = positioningType;
                PacketCount = 0;

                //Event Registry
                //Signalling
                Signalling.OnConnected += Signalling_Connected;
                Signalling.OnBindedUnbinded += Signalling_BindedUnbinded;
                Signalling.OnLogin += Signalling_Login;
                Signalling.OnLogout += Signalling_Logout;
                Signalling.OnDeafenUndeafen += Signalling_DeafenUndeafen;
                Signalling.OnMuteUnmute += Signalling_MuteUnmute;
                Signalling.OnAddChannel += Signalling_AddChannel;
                Signalling.OnJoinLeaveChannel += Signalling_JoinLeaveChannel;
                Signalling.OnDisconnected += Signalling_Disconnected;

                //Voice
                Voice.OnConnected += Voice_Connected;
                Voice.OnServerAudio += Voice_ServerAudio;
                Voice.OnDisconnected += Voice_Disconnected;

                //MCWSS
                MCWSS.OnConnect += WebsocketConnected;
                MCWSS.OnPlayerTravelled += WebsocketPlayerTravelled;
                MCWSS.OnDisconnect += WebsocketDisconnected;

                ConnectionState = ConnectionState.Connecting;
                await Signalling.Connect(iP, port, preferredKey, positioningType, Version);
            }
            catch(Exception ex)
            {
                Disconnect(ex.Message);
            }
        }

        public async Task SendAudio(byte[] data, int bytesRecorded)
        {
            if (IsDeafened || IsMuted || ConnectionState != ConnectionState.Connected) return;

            //Prevent overloading the highest max count of a uint.
            if (PacketCount >= uint.MaxValue)
                PacketCount = 0;

            //Count packets
            PacketCount++;

            byte[] audioEncodeBuffer = new byte[1000];
            var encodedBytes = Encoder.Encode(data, bytesRecorded, audioEncodeBuffer);
            byte[] audioTrimmed = audioEncodeBuffer.SkipLast(1000 - encodedBytes).ToArray();

            //Send the audio
            await Voice.SendPacketAsync(Core.Packets.Voice.ClientAudio.Create(PrivateId, PacketCount, audioTrimmed));
        }

        public async Task SetMute()
        {
            if (ConnectionState != ConnectionState.Connected) return;

            IsMuted = !IsMuted;
            await Signalling.SendPacketAsync(Core.Packets.Signalling.MuteUnmute.Create(PrivateId, 0, IsMuted), Signalling.Socket); //Mute and Deafen are based on IP & Private Id from client to server.
        }

        public async Task SetDeafen()
        {
            if (ConnectionState != ConnectionState.Connected) return;

            IsDeafened = !IsDeafened;
            await Signalling.SendPacketAsync(Core.Packets.Signalling.DeafenUndeafen.Create(PrivateId, 0, IsDeafened), Signalling.Socket); //Mute and Deafen are based on IP & Private Id from client to server.
        }

        public async Task JoinChannel(VoiceCraftChannel channel, string password = "")
        {
            if (!channel.Joined)
            {
                await Signalling.SendPacketAsync(Core.Packets.Signalling.JoinLeaveChannel.Create(PrivateId, channel.ChannelId, password, true), Signalling.Socket);
            }
        }

        public async Task LeaveChannel(VoiceCraftChannel channel)
        {
            if (channel.Joined)
            {
                await Signalling.SendPacketAsync(Core.Packets.Signalling.JoinLeaveChannel.Create(PrivateId, channel.ChannelId, "", false), Signalling.Socket);
            }
        }

        public void Disconnect(string? reason = null, bool force = false)
        {
            if (ConnectionState == ConnectionState.Disconnected) return; //Already Disconnected.

            //Event Registry
            Signalling.OnConnected -= Signalling_Connected;
            Signalling.OnBindedUnbinded -= Signalling_BindedUnbinded;
            Signalling.OnLogin -= Signalling_Login;
            Signalling.OnLogout -= Signalling_Logout;
            Signalling.OnDeafenUndeafen -= Signalling_DeafenUndeafen;
            Signalling.OnMuteUnmute -= Signalling_MuteUnmute;
            Signalling.OnAddChannel -= Signalling_AddChannel;
            Signalling.OnJoinLeaveChannel -= Signalling_JoinLeaveChannel;
            Signalling.OnDisconnected -= Signalling_Disconnected;

            Voice.OnConnected -= Voice_Connected;
            Voice.OnServerAudio -= Voice_ServerAudio;
            Voice.OnDisconnected -= Voice_Disconnected;

            MCWSS.OnConnect -= WebsocketConnected;
            MCWSS.OnPlayerTravelled -= WebsocketPlayerTravelled;
            MCWSS.OnDisconnect -= WebsocketDisconnected;

            ClearParticipants();
            Channels.Clear();

            ConnectionState = ConnectionState.Disconnected;
            Signalling.Disconnect(force: force);
            Voice.Disconnect();
            MCWSS.Stop();
            OnDisconnected?.Invoke(reason);
            IsDeafened = false;
            IsMuted = false;
        }

        #region Event Methods
        //Signalling
        private void Signalling_Connected(ushort port, ushort publicId = 0, int privateId = 0)
        {
            OnSignallingConnected?.Invoke();
            if (!string.IsNullOrWhiteSpace(IP))
            {
                PublicId = publicId;
                PrivateId = privateId;
                _ = Voice.Connect(IP, port, ((IPEndPoint)Signalling.Socket.LocalEndPoint).Port, privateId);
            }
            else
            {
                Disconnect("IP WAS SOMEHOW EMPTY!");
            }
        }

        private void Signalling_BindedUnbinded(Core.Packets.Signalling.BindedUnbinded data, Socket socket)
        {
            if(data.Binded)
            {
                Name = data.Name;
                OnBinded?.Invoke(Name);
            }
        }

        private void Signalling_Login(Core.Packets.Signalling.Login data, Socket socket)
        {
            if(!Participants.ContainsKey(data.PublicId))
            {
                var participant = new VoiceCraftParticipant(data.Name, data.PublicId, AudioFormat, FrameSizeMS)
                {
                    IsDeafened = data.IsDeafened,
                    IsMuted = data.IsMuted
                };

                Participants.TryAdd(data.PublicId, participant);
                AudioOutput.AddMixerInput(participant.AudioOutput);
                OnParticipantJoined?.Invoke(participant, data.PublicId);
            }
        }

        private void Signalling_Logout(Core.Packets.Signalling.Logout data, Socket socket)
        {
            //Logout participant. If PrivateId is the same as this then disconnect.
            if (data.PrivateId == PrivateId)
            {
                Disconnect("Kicked or banned!");
            }
            else if(Participants.Remove(data.PublicId, out var participant))
            {
                AudioOutput.RemoveMixerInput(participant.AudioOutput);
                participant.Dispose();
                OnParticipantLeft?.Invoke(participant, data.PublicId);
            }
        }

        private void Signalling_DeafenUndeafen(Core.Packets.Signalling.DeafenUndeafen data, Socket socket)
        {
            if(Participants.TryGetValue(data.PublicId, out var participant))
            {
                participant.IsDeafened = data.Value;
                OnParticipantDeafenedStateChanged?.Invoke(participant, data.Value);
            }
        }

        private void Signalling_MuteUnmute(Core.Packets.Signalling.MuteUnmute data, Socket socket)
        {
            if (Participants.TryGetValue(data.PublicId, out var participant))
            {
                participant.IsMuted = data.Value;
                OnParticipantMutedStateChanged?.Invoke(participant, data.Value);
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

            if(PositioningType == PositioningTypes.ClientSided)
            {
                MCWSS.Start();
            }
        }

        private void Voice_ServerAudio(Core.Packets.Voice.ServerAudio data, EndPoint endPoint)
        {
            if(Participants.TryGetValue(data.PublicId, out var participant))
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

        //MCWSS
        private void WebsocketConnected(string Username)
        {
            if (ConnectionState != ConnectionState.Connected) return;

            _ = Signalling.SendPacketAsync(Core.Packets.Signalling.BindedUnbinded.Create(PrivateId, Username, true), Signalling.Socket);
            OnBinded?.Invoke(Username);
        }

        private void WebsocketPlayerTravelled(System.Numerics.Vector3 position, string Dimension)
        {
            if (ConnectionState != ConnectionState.Connected) return;

            _ = Voice.SendPacketAsync(Core.Packets.Voice.UpdatePosition.Create(PrivateId, position, Dimension));
        }

        private void WebsocketDisconnected()
        {
            if (ConnectionState != ConnectionState.Connected) return;

            _ = Signalling.SendPacketAsync(Core.Packets.Signalling.BindedUnbinded.Create(PrivateId, "", false), Signalling.Socket);
            ClearParticipants();
            Channels.Clear();
            OnUnbinded?.Invoke();
        }
        #endregion

        private void ClearParticipants()
        {
            foreach (var participant in Participants)
            {
                participant.Value.Dispose();
            }
            Participants.Clear();
        }

        ~VoiceCraftClient()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    if (ConnectionState == ConnectionState.Connected)
                        Disconnect(); //Disconnect before disposing.

                    foreach (var participant in Participants)
                    {
                        participant.Value.Dispose();
                    }

                    Signalling.Dispose();
                    MCWSS.Dispose();
                    Voice.Dispose();
                    Encoder.Dispose();
                    ConnectionState = ConnectionState.Disconnected;
                }
                IsDisposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        //Static methods
        public static async Task<string> PingAsync(string IP, int Port)
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            var message = "";
            var pingTime = DateTime.UtcNow;
            byte[]? packetBuffer = null;
            byte[] lengthBuffer = new byte[2];
            try
            {
                if (socket.ConnectAsync(IP, Port).Wait(5000))
                {
                    var stream = new NetworkStream(socket);
                    var pingPacket = Core.Packets.Signalling.Ping.Create(string.Empty).GetPacketStream();
                    await socket.SendAsync(BitConverter.GetBytes((ushort)pingPacket.Length), SocketFlags.None);
                    await socket.SendAsync(pingPacket, SocketFlags.None);
                    //TCP Is Annoying
                    var bytes = await stream.ReadAsync(lengthBuffer, 0, lengthBuffer.Length).ConfigureAwait(false);
                    if (bytes == 0)
                    {
                        stream.Dispose();
                        throw new Exception("Socket closed"); //Socket is closed.
                    }

                    ushort packetLength = SignallingPacket.GetPacketLength(lengthBuffer);
                    //If packets are an invalid length then we break out to prevent memory exceptions and disconnect the client.
                    if (packetLength > 1024)
                    {
                        stream.Dispose();
                        throw new Exception("Invalid packet received.");
                    }//Packets will never be bigger than 500 bytes but the hard limit is 1024 bytes/1mb

                    packetBuffer = new byte[packetLength];

                    //Read until packet is fully received
                    int offset = 0;
                    while (offset < packetLength)
                    {
                        int bytesRead = await stream.ReadAsync(packetBuffer, offset, packetLength).ConfigureAwait(false);
                        if (bytesRead == 0) break; //Socket is closed.

                        offset += bytesRead;
                    }
                    var packet = new SignallingPacket(packetBuffer);
                    var pingTimeMS = DateTime.UtcNow.Subtract(pingTime).TotalMilliseconds;
                    if (packet.PacketType == SignallingPacketTypes.Ping)
                    {
                        var data = (Core.Packets.Signalling.Ping)packet.PacketData;
                        message = $"{data.ServerData}\nPing Time: {Math.Floor(pingTimeMS)}ms";
                    }
                    else
                        message = $"Unexpected packet received\nPing Time: {Math.Floor(pingTimeMS)}ms";

                    stream.Dispose();
                }
                else
                {
                    var pingTimeMS = DateTime.UtcNow.Subtract(pingTime).TotalMilliseconds;
                    message = $"Timed out\nPing Time: {Math.Floor(pingTimeMS)}ms";
                }

                socket.Disconnect(false);
                socket.Close();
            }
            catch (Exception ex)
            {
                message = $"Error: {ex.Message}";
            }

            return message;
        }
    }

    public enum ConnectionState
    {
        Connecting,
        Connected,
        Disconnected
    }
}
