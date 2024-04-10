using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using OpusSharp;
using System.Collections.Concurrent;
using System.Net.Sockets;
using VoiceCraft.Core;
using VoiceCraft.Core.Packets;
using VoiceCraft.Core.Packets.VoiceCraft;

namespace VoiceCraft.Maui.VoiceCraft
{
    public class VoiceCraftClient : Disposable
    {
        //Private Variables
        public const string Version = "v1.0.4";
        private bool IsConnected;
        private uint PacketCount;
        private OpusEncoder Encoder;
        private int FrameSizeMS;

        //Variables
        public ConcurrentDictionary<short, VoiceCraftParticipant> Participants { get; set; } = new ConcurrentDictionary<short, VoiceCraftParticipant>();
        public ConcurrentDictionary<byte, Channel> Channels { get; set; } = new ConcurrentDictionary<byte, Channel>();
        public Network.Sockets.VoiceCraft VoiceCraftSocket { get; set; } = new Network.Sockets.VoiceCraft();
        public Network.Sockets.MCWSS MCWSS { get; set; }
        public short Key { get; private set; }
        public Channel? JoinedChannel { get; private set; }
        public PositioningTypes PositioningType { get; private set; }

        //Audio Variables
        public bool Muted { get; private set; }
        public bool Deafened { get; private set; }
        public bool LinearProximity { get; set; }
        public bool DirectionalHearing { get; set; }
        public MixingSampleProvider AudioOutput { get; }
        public WaveFormat AudioFormat { get; }
        public WaveFormat PlaybackFormat { get; }

        #region Delegates
        public delegate void Connected();
        public delegate void Disconnected(string? reason = null);
        public delegate void Deny(string? reason = null);
        public delegate void Failed(Exception ex);

        public delegate void Binded(string name);
        public delegate void Unbinded();
        public delegate void ParticipantJoined(VoiceCraftParticipant participant);
        public delegate void ParticipantLeft(VoiceCraftParticipant participant);
        public delegate void ParticipantUpdated(VoiceCraftParticipant participant);
        public delegate void ChannelAdded(Channel channel);
        public delegate void ChannelRemoved(Channel channel);
        public delegate void ChannelJoined(Channel channel);
        public delegate void ChannelLeft(Channel channel);
        #endregion

        #region Events
        public event Connected? OnConnected;
        public event Disconnected? OnDisconnected;
        public event Deny? OnDeny;
        public event Failed? OnFailed;

        public event Binded? OnBinded;
        public event Unbinded? OnUnbinded;
        public event ParticipantJoined? OnParticipantJoined;
        public event ParticipantLeft? OnParticipantLeft;
        public event ParticipantUpdated? OnParticipantUpdated;
        public event ChannelAdded? OnChannelAdded;
        public event ChannelRemoved? OnChannelRemoved;
        public event ChannelJoined? OnChannelJoined;
        public event ChannelLeft? OnChannelLeft;
        #endregion

        public VoiceCraftClient(WaveFormat audioFormat, int frameSizeMS = 20, int MCWSSPort = 8080)
        {
            MCWSS = new Network.Sockets.MCWSS(MCWSSPort);

            AudioFormat = audioFormat;
            PlaybackFormat = WaveFormat.CreateIeeeFloatWaveFormat(AudioFormat.SampleRate, 2);
            FrameSizeMS = frameSizeMS;

            Encoder = new OpusEncoder(AudioFormat.SampleRate, AudioFormat.Channels, OpusSharp.Enums.Application.VOIP)
            {
                Bitrate = 32000,
                PacketLossPerc = 50
            };
            AudioOutput = new MixingSampleProvider(PlaybackFormat) { ReadFully = true };

            MCWSS.OnConnect += MCWSSConnected;
            MCWSS.OnDisconnect += MCWSSDisconnected;
            MCWSS.OnPlayerTravelled += MCWSSPlayerTravelled;

            VoiceCraftSocket.OnConnected += VoiceCraftSocketConnected;
            VoiceCraftSocket.OnDisconnected += VoiceCraftSocketDisconnected;
            VoiceCraftSocket.OnBindedReceived += VoiceCraftSocketBinded;
            VoiceCraftSocket.OnParticipantLeftReceived += VoiceCraftSocketParticipantLeft;
            VoiceCraftSocket.OnParticipantJoinedReceived += VoiceCraftSocketParticipantJoined;
            VoiceCraftSocket.OnAddChannelReceived += VoiceCraftSocketAddChannel;
            VoiceCraftSocket.OnRemoveChannelReceived += VoiceCraftSocketRemoveChannel;
            VoiceCraftSocket.OnMuteReceived += VoiceCraftSocketMute;
            VoiceCraftSocket.OnUnmuteReceived += VoiceCraftSocketUnmute;
            VoiceCraftSocket.OnDeafenReceived += VoiceCraftSocketDeafen;
            VoiceCraftSocket.OnUndeafenReceived += VoiceCraftSocketUndeafen;
            VoiceCraftSocket.OnJoinChannelReceived += VoiceCraftSocketJoinChannel;
            VoiceCraftSocket.OnLeaveChannelReceived += VoiceCraftSocketLeaveChannel;
            VoiceCraftSocket.OnServerAudioReceived += VoiceCraftSocketServerAudio;
            VoiceCraftSocket.OnDenyReceived += VoiceCraftSocketDenyReceived;
        }

        #region Methods
        public void Connect(string ip, ushort port, short key, PositioningTypes positioningType)
        {
            if (IsDisposed) throw new ObjectDisposedException(nameof(VoiceCraftClient));

            PositioningType = positioningType;
            _ = Task.Run(async () =>
            {
                try
                {
                    await VoiceCraftSocket.ConnectAsync(ip, port, key, positioningType, Version);
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    OnFailed?.Invoke(ex);
                }
            });
        }

        public void Disconnect(string? reason = null)
        {
            if (IsDisposed && !IsConnected) throw new ObjectDisposedException(nameof(VoiceCraftClient));

            VoiceCraftSocket.DisconnectAsync().Wait();
            MCWSS?.Stop();
            ClearParticipants();

            if (IsConnected) OnDisconnected?.Invoke(reason);
            IsConnected = false;
        }

        public void SetMute(bool mute)
        {
            if(mute != Muted)
            {
                VoiceCraftPacket packet = mute ? new Core.Packets.VoiceCraft.Mute() : new Core.Packets.VoiceCraft.Unmute();
                VoiceCraftSocket.Send(packet);
                Muted = mute;
            }
        }

        public void SetDeafen(bool deafen)
        {
            if (deafen != Deafened)
            {
                Deafened = deafen;
                VoiceCraftPacket packet = deafen ? new Core.Packets.VoiceCraft.Deafen() : new Core.Packets.VoiceCraft.Undeafen();
                VoiceCraftSocket.Send(packet);
            }
        }

        public void JoinChannel(Channel channel, string password = "")
        {
            var c = Channels.FirstOrDefault(x => x.Value == channel);
            if (c.Value != null && JoinedChannel != channel)
            {
                VoiceCraftSocket.Send(new Core.Packets.VoiceCraft.JoinChannel() { ChannelId = c.Key, Password = password });
            }
        }

        public void LeaveChannel()
        {
            var c = Channels.FirstOrDefault(x => x.Value == JoinedChannel);
            if (c.Value != null && JoinedChannel != null)
            {
                VoiceCraftSocket.Send(new Core.Packets.VoiceCraft.LeaveChannel());
            }
        }

        public void SendAudio(byte[] audio, int bytesRecorded)
        {
            if (Deafened || Muted || !IsConnected) return;
            PacketCount++;

            byte[] audioEncodeBuffer = new byte[1000];
            var encodedBytes = Encoder.Encode(audio, bytesRecorded, audioEncodeBuffer);
            byte[] audioTrimmed = audioEncodeBuffer.SkipLast(1000 - encodedBytes).ToArray();

            VoiceCraftSocket.Send(new Core.Packets.VoiceCraft.ClientAudio() { Audio = audioTrimmed, PacketCount = PacketCount });
        }

        private void ClearParticipants()
        {
            foreach (var participant in Participants)
            {
                participant.Value.Dispose();
            }
            Participants.Clear();
        }

        protected override void Dispose(bool disposing)
        {
            if(disposing)
            {
                if (IsConnected)
                    Disconnect();

                VoiceCraftSocket.Dispose();
                MCWSS?.Dispose();
                Encoder.Dispose();
                Channels.Clear();
                Participants.Clear();
            }
        }
        #endregion

        #region Event Methods
        private void VoiceCraftSocketConnected(short key)
        {
            Key = key;
            IsConnected = true;
            OnConnected?.Invoke();

            if (PositioningType == PositioningTypes.ClientSided) MCWSS.Start();
        }
        
        private void VoiceCraftSocketDisconnected(string? reason = null)
        {
            Disconnect(reason);
        }

        private void VoiceCraftSocketBinded(Core.Packets.VoiceCraft.Binded data, Network.NetPeer peer)
        {
            OnBinded?.Invoke(data.Name);
        }

        private void VoiceCraftSocketParticipantJoined(Core.Packets.VoiceCraft.ParticipantJoined data, Network.NetPeer peer)
        {
            var participant = new VoiceCraftParticipant(data.Name, AudioFormat, FrameSizeMS) { Deafened = data.IsDeafened, Muted = data.IsMuted };
            if (Participants.TryAdd(data.Key, participant))
            {
                AudioOutput.AddMixerInput(participant.AudioOutput);
                OnParticipantJoined?.Invoke(participant);
            }
        }

        private void VoiceCraftSocketParticipantLeft(Core.Packets.VoiceCraft.ParticipantLeft data, Network.NetPeer peer)
        {
            if(Participants.TryRemove(data.Key, out var participant))
            {
                AudioOutput.RemoveMixerInput(participant.AudioOutput);
                OnParticipantLeft?.Invoke(participant);
            }
        }

        private void VoiceCraftSocketAddChannel(Core.Packets.VoiceCraft.AddChannel data, Network.NetPeer peer)
        {
            var channel = new Channel() { Name = data.Name, Password = data.RequiresPassword ? "Required" : string.Empty };
            if(Channels.TryAdd(data.ChannelId, channel))
            {
                OnChannelAdded?.Invoke(channel);
            }
        }

        private void VoiceCraftSocketRemoveChannel(Core.Packets.VoiceCraft.RemoveChannel data, Network.NetPeer peer)
        {
            if(Channels.TryRemove(data.ChannelId, out var channel))
            {
                OnChannelRemoved?.Invoke(channel);
            }
        }

        private void VoiceCraftSocketMute(Core.Packets.VoiceCraft.Mute data, Network.NetPeer peer)
        {
            if(Participants.TryGetValue(data.Key, out var participant) && !participant.Muted)
            {
                participant.Muted = true;
                OnParticipantUpdated?.Invoke(participant);
            }
        }

        private void VoiceCraftSocketUnmute(Core.Packets.VoiceCraft.Unmute data, Network.NetPeer peer)
        {
            if (Participants.TryGetValue(data.Key, out var participant) && participant.Muted)
            {
                participant.Muted = false;
                OnParticipantUpdated?.Invoke(participant);
            }
        }

        private void VoiceCraftSocketDeafen(Core.Packets.VoiceCraft.Deafen data, Network.NetPeer peer)
        {
            if (Participants.TryGetValue(data.Key, out var participant) && !participant.Deafened)
            {
                participant.Deafened = true;
                OnParticipantUpdated?.Invoke(participant);
            }
        }

        private void VoiceCraftSocketUndeafen(Core.Packets.VoiceCraft.Undeafen data, Network.NetPeer peer)
        {
            if (Participants.TryGetValue(data.Key, out var participant) && participant.Deafened)
            {
                participant.Deafened = false;
                OnParticipantUpdated?.Invoke(participant);
            }
        }

        private void VoiceCraftSocketJoinChannel(Core.Packets.VoiceCraft.JoinChannel data, Network.NetPeer peer)
        {
            if(Channels.TryGetValue(data.ChannelId, out var channel) && channel != JoinedChannel)
            {
                ClearParticipants();
                JoinedChannel = channel;
                OnChannelJoined?.Invoke(channel);
            }
        }

        private void VoiceCraftSocketLeaveChannel(Core.Packets.VoiceCraft.LeaveChannel data, Network.NetPeer peer)
        {
            if (JoinedChannel != null)
            {
                ClearParticipants();
                OnChannelLeft?.Invoke(JoinedChannel);
                JoinedChannel = null;
            }
        }

        private void VoiceCraftSocketServerAudio(Core.Packets.VoiceCraft.ServerAudio data, Network.NetPeer peer)
        {
            if (Participants.TryGetValue(data.Key, out var participant))
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

        private void VoiceCraftSocketDenyReceived(Core.Packets.VoiceCraft.Deny data, Network.NetPeer peer)
        {
            if(IsConnected)
            {
                OnDeny?.Invoke(data.Reason);
            }
        }
        #endregion

        #region MCWSS
        private void MCWSSConnected(string Username)
        {
            if (!IsConnected) return;

            VoiceCraftSocket.Send(new Core.Packets.VoiceCraft.Binded() { Name = Username });
            OnBinded?.Invoke(Username);
        }

        private void MCWSSPlayerTravelled(System.Numerics.Vector3 position, string Dimension)
        {
            if (!IsConnected) return;

            VoiceCraftSocket.Send(new UpdatePosition() { Position = position, EnvironmentId = Dimension });
        }

        private void MCWSSDisconnected()
        {
            if (!IsConnected) return;

            VoiceCraftSocket.Send(new Core.Packets.VoiceCraft.Unbinded());
            ClearParticipants(); //Clear the entire list
            Channels.Clear();
            OnUnbinded?.Invoke();
        }
        #endregion

        public static async Task<string> PingAsync(string IP, int Port)
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            var pingTime = DateTime.UtcNow;
            byte[] packetBuffer = new byte[250];
            string message = string.Empty;

            var PacketRegistry = new PacketRegistry();
            PacketRegistry.RegisterPacket((byte)VoiceCraftPacketTypes.PingInfo, typeof(PingInfo));

            try
            {
                socket.Connect(IP, Port);

                var buffer = new List<byte>();
                var ping = new PingInfo();
                ping.WritePacket(ref buffer);
                await socket.SendAsync(buffer.ToArray());

                if (socket.ReceiveAsync(packetBuffer).Wait(5000))
                {
                    var packet = (PingInfo)PacketRegistry.GetPacketFromDataStream(packetBuffer);
                    var pingTimeMS = DateTime.UtcNow.Subtract(pingTime).TotalMilliseconds;

                    var positioningType = string.Empty;
                    switch(packet.PositioningType)
                    {
                        case PositioningTypes.ServerSided: positioningType = "Server"; break;
                        case PositioningTypes.ClientSided: positioningType = "Client"; break;
                        case PositioningTypes.Unknown: positioningType = "Hybrid"; break;
                    }

                    message = $"MOTD: {packet.MOTD}\n Connected Participants: {packet.ConnectedParticipants}\n Positioning Type: {positioningType}\nPing Time: {Math.Floor(pingTimeMS)}ms";
                }
                else
                {
                    var pingTimeMS = DateTime.UtcNow.Subtract(pingTime).TotalMilliseconds;
                    message = $"Timed out\nPing Time: {Math.Floor(pingTimeMS)}ms";
                }

                socket.Dispose();
            }
            catch (Exception ex)
            {
                message = $"Error: {ex.Message}";
            }

            return message;
        }
    }
}
