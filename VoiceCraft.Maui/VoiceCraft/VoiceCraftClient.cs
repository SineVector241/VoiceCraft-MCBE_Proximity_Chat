using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using OpusSharp;
using System.Collections.Concurrent;
using VoiceCraft.Core;
using VoiceCraft.Core.Packets;

namespace VoiceCraft.Maui.VoiceCraft
{
    public class VoiceCraftClient : Disposable
    {
        //Private Variables
        public const string Version = "v1.0.4";
        private string IP = string.Empty;
        private ushort Port = 9050;
        private bool IsConnected;
        private uint PacketCount;
        private PositioningTypes PositioningType;
        private OpusEncoder Encoder;
        private int FrameSizeMS;

        //Variables
        public ConcurrentDictionary<short, VoiceCraftParticipant> Participants { get; set; } = new ConcurrentDictionary<short, VoiceCraftParticipant>();
        public ConcurrentDictionary<byte, Channel> Channels { get; set; } = new ConcurrentDictionary<byte, Channel>();
        public Network.Sockets.VoiceCraft VoiceCraftSocket { get; set; } = new Network.Sockets.VoiceCraft();
        //MCWSS Socket Here
        public ushort MCWSSPort { get; set; } = 8080;
        public short Key { get; private set; }
        public Channel? JoinedChannel { get; private set; }

        //Audio Variables
        public bool IsMuted { get; private set; }
        public bool IsDeafened { get; private set; }
        public bool LinearProximity { get; set; }
        public bool DirectionalHearing { get; set; }
        public MixingSampleProvider AudioOutput { get; }
        public WaveFormat AudioFormat { get; }
        public WaveFormat PlaybackFormat { get; }

        #region Delegates
        public delegate void Connected();
        public delegate void Disconnected(string? reason = null);
        public delegate void Failed(Exception ex);

        public delegate void Binded(string name);
        public delegate void ParticipantJoined(VoiceCraftParticipant participant);
        public delegate void ParticipantLeft(VoiceCraftParticipant participant);
        public delegate void ChannelAdded(Channel channel);
        public delegate void ChannelRemoved(Channel channel);
        #endregion

        #region Events
        public event Connected? OnConnected;
        public event Disconnected? OnDisconnected;
        public event Failed? OnFailed;

        public event Binded? OnBinded;
        public event ParticipantJoined? OnParticipantJoined;
        public event ParticipantLeft? OnParticipantLeft;
        public event ChannelAdded? OnChannelAdded;
        public event ChannelRemoved? OnChannelRemoved;
        #endregion

        public VoiceCraftClient(WaveFormat audioFormat, int frameSizeMS = 20)
        {
            AudioFormat = audioFormat;
            PlaybackFormat = WaveFormat.CreateIeeeFloatWaveFormat(AudioFormat.SampleRate, 2);
            FrameSizeMS = frameSizeMS;

            Encoder = new OpusEncoder(AudioFormat.SampleRate, AudioFormat.Channels, OpusSharp.Enums.Application.VOIP)
            {
                Bitrate = 32000,
                PacketLossPerc = 50
            };
            AudioOutput = new MixingSampleProvider(PlaybackFormat) { ReadFully = true };

            VoiceCraftSocket.OnConnected += VoiceCraftSocketConnected;
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
        }

        public void Connect(string ip, ushort port, short key, PositioningTypes positioningType)
        {
            if (IsDisposed) throw new ObjectDisposedException(nameof(VoiceCraftClient));

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
            if (IsDisposed) throw new ObjectDisposedException(nameof(VoiceCraftClient));

            VoiceCraftSocket.DisconnectAsync().Wait();

            if (!IsConnected) OnDisconnected?.Invoke(reason);
            IsConnected = false;
        }

        #region Event Methods
        private void VoiceCraftSocketConnected(short key)
        {
            Key = key;
            IsConnected = true;
            OnConnected?.Invoke();
        }

        private void VoiceCraftSocketBinded(Core.Packets.VoiceCraft.Binded data, Network.NetPeer peer)
        {
            OnBinded?.Invoke(data.Name);
        }

        private void VoiceCraftSocketParticipantJoined(Core.Packets.VoiceCraft.ParticipantJoined data, Network.NetPeer peer)
        {
            var participant = new VoiceCraftParticipant(data.Name) { Deafened = data.IsDeafened, Muted = data.IsMuted };
            if (Participants.TryAdd(data.Key, participant))
            {
                OnParticipantJoined?.Invoke(participant);
            }
        }

        private void VoiceCraftSocketParticipantLeft(Core.Packets.VoiceCraft.ParticipantLeft data, Network.NetPeer peer)
        {
            if(Participants.TryRemove(data.Key, out var participant))
            {
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
            }
        }

        private void VoiceCraftSocketUnmute(Core.Packets.VoiceCraft.Unmute data, Network.NetPeer peer)
        {
            if (Participants.TryGetValue(data.Key, out var participant) && participant.Muted)
            {
                participant.Muted = false;
            }
        }

        private void VoiceCraftSocketDeafen(Core.Packets.VoiceCraft.Deafen data, Network.NetPeer peer)
        {
            if (Participants.TryGetValue(data.Key, out var participant) && !participant.Deafened)
            {
                participant.Deafened = true;
            }
        }

        private void VoiceCraftSocketUndeafen(Core.Packets.VoiceCraft.Undeafen data, Network.NetPeer peer)
        {
            if (Participants.TryGetValue(data.Key, out var participant) && participant.Deafened)
            {
                participant.Deafened = false;
            }
        }

        private void VoiceCraftSocketJoinChannel(Core.Packets.VoiceCraft.JoinChannel data, Network.NetPeer peer)
        {
            if(Channels.TryGetValue(data.ChannelId, out var channel) && channel != JoinedChannel)
            {
                JoinedChannel = channel;
            }
        }

        private void VoiceCraftSocketLeaveChannel(Core.Packets.VoiceCraft.LeaveChannel data, Network.NetPeer peer)
        {
            if (JoinedChannel != null)
            {
                JoinedChannel = null;
            }
        }
        #endregion
    }
}
