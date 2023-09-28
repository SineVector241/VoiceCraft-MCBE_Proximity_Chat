using Concentus.Structs;
using NAudio.Wave.SampleProviders;
using NAudio.Wave;
using System.Threading;
using VoiceCraft.Core.Packets;
using System.Collections.Concurrent;
using System;
using System.Linq;
using VoiceCraft.Core.Client.Sockets;
using VoiceCraft.Windows.Network.Sockets;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Diagnostics;

namespace VoiceCraft.Core.Client
{
    public class VoiceCraftClient : IDisposable
    {
        #region Fields
        //Constants
        public const int SampleRate = 48000;

        //Variables
        private CancellationTokenSource CTS;
        public string IP { get; private set; } = string.Empty;
        public int Port { get; private set; }
        public int MCWSSPort { get; private set; }
        public ushort LoginKey { get; private set; }
        public string Version { get; private set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public PositioningTypes PositioningType { get; private set; }
        public bool IsMuted { get; private set; }
        public bool IsDeafened { get; private set; }

        //Object states
        public bool IsConnected { get; private set; }
        public bool IsDisposed { get; private set; }

        //Changeable Variables
        public bool DirectionalHearing { get; set; }
        public bool LinearVolume { get; set; }

        //Server Data
        public ConcurrentDictionary<ushort, VoiceCraftParticipant> Participants { get; private set; } = new ConcurrentDictionary<ushort, VoiceCraftParticipant>();
        public uint PacketCount { get; private set; }

        //Audio Variables
        public int RecordLengthMS { get; }
        public WaveFormat RecordFormat { get; } = new WaveFormat(SampleRate, 1);
        public WaveFormat PlaybackFormat { get; } = WaveFormat.CreateIeeeFloatWaveFormat(SampleRate, 2);
        public MixingSampleProvider Mixer { get; }

        //Codec Encoder
        private readonly OpusEncoder Encoder;

        //Sockets
        public SignallingSocket Signalling { get; private set; }
        public VoiceSocket Voice { get; private set; }
        public MCWSSSocket MCWSS { get; private set; }

        //Delegates
        public delegate void Connected();
        public delegate void Binded(string? name);
        public delegate void Unbinded();
        public delegate void ParticipantJoined(VoiceCraftParticipant participant, ushort key);
        public delegate void ParticipantLeft(VoiceCraftParticipant participant, ushort key);
        public delegate void ParticipantUpdated(VoiceCraftParticipant participant, ushort key);
        public delegate void Disconnected(string? reason);

        //Events
        public event Connected? OnConnected;
        public event Binded? OnBinded;
        public event Unbinded? OnUnbinded;
        public event ParticipantJoined? OnParticipantJoined;
        public event ParticipantLeft? OnParticipantLeft;
        public event ParticipantUpdated? OnParticipantUpdated;
        public event Disconnected? OnDisconnected;
        #endregion
        public VoiceCraftClient(ushort LoginKey, PositioningTypes PositioningType, string Version, int RecordLengthMS = 40, int MCWSSPort = 8080)
        {
            //Setup variables
            this.LoginKey = LoginKey;
            this.Version = Version;
            this.PositioningType = PositioningType;
            this.RecordLengthMS = RecordLengthMS;
            this.MCWSSPort = MCWSSPort;

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
            Signalling = new SignallingSocket();
            Voice = new VoiceSocket();
            MCWSS = new MCWSSSocket(MCWSSPort);

            //Event Registration in login order.
            //Signalling
            Signalling.OnAcceptPacketReceived += SignallingAccept;
            Signalling.OnBindedPacketReceived += SignallingBinded;
            Signalling.OnLoginPacketReceived += SignallingLogin;
            Signalling.OnLogoutPacketReceived += SignallingLogout;
            Signalling.OnDeafenPacketReceived += SignallingDeafen;
            Signalling.OnUndeafenPacketReceived += SignallingUndeafen;
            Signalling.OnMutePacketReceived += SignallingMute;
            Signalling.OnUnmutePacketReceived += SignallingUnmute;

            //Voice
            Voice.OnAcceptPacketReceived += VoiceAccept;
            Voice.OnServerAudioPacketReceived += VoiceServerAudio;

            //MCWSS
            MCWSS.OnConnect += WebsocketConnected;
            MCWSS.OnPlayerTravelled += WebsocketPlayerTravelled;
            MCWSS.OnDisconnect += WebsocketDisconnected;

            //Socket Disconnections
            Signalling.OnSocketDisconnected += SocketDisconnected;
            Voice.OnSocketDisconnected += SocketDisconnected;
        }

        private void SocketDisconnected(string reason)
        {
            Disconnect(reason);
        }

        //Signalling Event Methods
        #region Signalling
        private void SignallingAccept(Packets.Signalling.Accept packet)
        {
            LoginKey = packet.LoginKey;
            _ = Voice.ConnectAsync(IP, packet.VoicePort, LoginKey);
        }

        private void SignallingBinded(Packets.Signalling.Binded packet)
        {
            OnBinded?.Invoke(packet.Name);
        }

        private void SignallingLogin(Packets.Signalling.Login packet)
        {
            if (!Participants.ContainsKey(packet.LoginKey))
            {
                var participant = new VoiceCraftParticipant(packet.Name, RecordFormat, RecordLengthMS)
                {
                    Muted = packet.IsMuted,
                    Deafened = packet.IsDeafened
                };
                Participants.TryAdd(packet.LoginKey, participant);
                Mixer.AddMixerInput(participant.AudioProvider);
                OnParticipantJoined?.Invoke(participant, packet.LoginKey);
            }
        }

        private void SignallingLogout(Packets.Signalling.Logout packet)
        {
            //Logout participant. If LoginKey is the same as this then disconnect.
            if (packet.LoginKey == LoginKey)
            {
                Disconnect();
                OnDisconnected?.Invoke("Kicked or banned.");
            }
            else
            {
                Participants.TryRemove(packet.LoginKey, out var participant);
                if (participant != null)
                {
                    Mixer.RemoveMixerInput(participant.AudioProvider);
                    OnParticipantLeft?.Invoke(participant, packet.LoginKey);
                }
            }
        }

        private void SignallingDeafen(Packets.Signalling.Deafen packet)
        {
            Participants.TryGetValue(packet.LoginKey, out var participant);
            if (participant != null)
            {
                participant.Deafened = true;
                OnParticipantUpdated?.Invoke(participant, packet.LoginKey);
            }
        }

        private void SignallingUndeafen(Packets.Signalling.Undeafen packet)
        {
            Participants.TryGetValue(packet.LoginKey, out var participant);
            if (participant != null)
            {
                participant.Deafened = false;
                OnParticipantUpdated?.Invoke(participant, packet.LoginKey);
            }
        }

        private void SignallingMute(Packets.Signalling.Mute packet)
        {
            Participants.TryGetValue(packet.LoginKey, out var participant);
            if (participant != null)
            {
                participant.Muted = true;
                OnParticipantUpdated?.Invoke(participant, packet.LoginKey);
            }
        }

        private void SignallingUnmute(Packets.Signalling.Unmute packet)
        {
            Participants.TryGetValue(packet.LoginKey, out var participant);
            if (participant != null)
            {
                participant.Muted = false;
                OnParticipantUpdated?.Invoke(participant, packet.LoginKey);
            }
        }
        #endregion
        //Voice Event Methods
        #region Voice
        private void VoiceAccept(Packets.Voice.Accept packet)
        {
            IsConnected = true;
            OnConnected?.Invoke();
        }

        private void VoiceServerAudio(Packets.Voice.ServerAudio packet)
        {
            //Add audio to a participant
            Participants.TryGetValue(packet.LoginKey, out var participant);
            if (participant != null)
            {
                participant.ProximityVolume = LinearVolume ? (float)((Math.Exp(packet.Volume) - 1) / (Math.E - 1)) : packet.Volume;
                participant.EchoProvider.EchoFactor = packet.EchoFactor;
                participant.LowpassProvider.Enabled = packet.Muffled;
                if (PositioningType != PositioningTypes.ClientSided && DirectionalHearing)
                {
                    participant.AudioProvider.RightVolume = (float)(0.5 + Math.Cos(packet.Rotation) * 0.5);
                    participant.AudioProvider.LeftVolume = (float)(0.5 - Math.Cos(packet.Rotation) * 0.5);
                }
                participant.AddAudioSamples(packet.Audio, packet.PacketCount);
            }
        }
        #endregion
        //Websocket Event Methods
        #region MCWSS
        private void WebsocketConnected(string Username)
        {
            if (!IsConnected) return;

            _ = Signalling.SendPacketAsync(new SignallingPacket()
            {
                PacketType = SignallingPacketTypes.Binded,
                PacketData = new Packets.Signalling.Binded()
                {
                    Name = Username
                }
            });
            OnBinded?.Invoke(Username);
        }

        private void WebsocketPlayerTravelled(System.Numerics.Vector3 position, string Dimension)
        {
            if(!IsConnected) return;

            _ = Voice.SendPacketAsync(new VoicePacket()
            {
                PacketType = VoicePacketTypes.UpdatePosition,
                PacketData = new Packets.Voice.UpdatePosition()
                {
                    EnvironmentId = Dimension,
                    Position = position
                }
            });
        }

        private void WebsocketDisconnected()
        {
            if (!IsConnected) return;

            _ = Signalling.SendPacketAsync(new SignallingPacket()
            {
                PacketType = SignallingPacketTypes.Unbinded,
                PacketData = new Packets.Signalling.Unbinded()
            });
            Participants.Clear(); //Clear the entire list
            OnUnbinded?.Invoke();
        }
        #endregion
        //Public Methods
        #region Public Methods
        public void Connect(string IP, int Port)
        {
            if(IsDisposed) throw new ObjectDisposedException(nameof(VoiceCraftClient));
            if (IsConnected) throw new InvalidOperationException("You must disconnect before connecting!");

            this.IP = IP;
            this.Port = Port;

            if (CTS.IsCancellationRequested)
            {
                CTS.Dispose();
                CTS = new CancellationTokenSource();
            }
            _ = Signalling.ConnectAsync(IP, Port, LoginKey, PositioningType, Version);
        }

        public void Disconnect(string? Reason = null)
        {
            try
            {
                if(!CTS.IsCancellationRequested)
                {
                    CTS.Cancel();
                    Signalling.Disconnect();
                    Voice.Disconnect();
                    MCWSS.Stop();
                    if (!string.IsNullOrWhiteSpace(Reason)) OnDisconnected?.Invoke(Reason);
                    IsConnected = false;
                    IsMuted = false;
                    IsDeafened = false;
                }
            }
            catch(Exception ex)
            {
#if DEBUG
                Debug.WriteLine(ex);
#endif
            }
        }

        public void SendAudio(byte[] Data, int BytesRecorded)
        {
            if (IsDeafened || IsMuted || !IsConnected) return;

            //Prevent overloading the highest max count of a uint.
            if (PacketCount >= uint.MaxValue)
                PacketCount = 0;

            //Count packets
            PacketCount++;

            byte[] audioEncodeBuffer = new byte[1000];
            short[] pcm = BytesToShorts(Data, 0, BytesRecorded);
            var encodedBytes = Encoder.Encode(pcm, 0, pcm.Length, audioEncodeBuffer, 0, audioEncodeBuffer.Length);
            byte[] audioTrimmed = audioEncodeBuffer.SkipLast(1000 - encodedBytes).ToArray();

            //Packet creation.
            VoicePacket packet = new VoicePacket()
            {
                PacketType = VoicePacketTypes.ClientAudio,
                PacketData = new Packets.Voice.ClientAudio()
                {
                    Audio = audioTrimmed,
                    PacketCount = PacketCount
                }
            }; //Audio packet stuff here.
            _ = Voice.SendPacketAsync(packet);
        }

        public void SetMute(bool Muted)
        {
            if(!IsConnected) return;

            IsMuted = Muted;
            if(IsMuted)
            {
                _ = Signalling.SendPacketAsync(new SignallingPacket()
                {
                    PacketType = SignallingPacketTypes.Mute,
                    PacketData = new Packets.Signalling.Mute()
                });
            }
            else
            {
                _ = Signalling.SendPacketAsync(new SignallingPacket()
                {
                    PacketType = SignallingPacketTypes.Unmute,
                    PacketData = new Packets.Signalling.Unmute()
                });
            }
        }

        public void SetDeafen(bool Deafened)
        {
            if (!IsConnected) return;

            IsDeafened = Deafened;
            if (IsDeafened)
            {
                _ = Signalling.SendPacketAsync(new SignallingPacket()
                {
                    PacketType = SignallingPacketTypes.Deafen,
                    PacketData = new Packets.Signalling.Deafen()
                });
            }
            else
            {
                _ = Signalling.SendPacketAsync(new SignallingPacket()
                {
                    PacketType = SignallingPacketTypes.Undeafen,
                    PacketData = new Packets.Signalling.Undeafen()
                });
            }
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
                    var pingPacket = new SignallingPacket() { PacketType = SignallingPacketTypes.Ping, PacketData = new Packets.Signalling.Ping() { } }.GetPacketStream();
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
                        var data = (Packets.Signalling.Ping)packet.PacketData;
                        message = $"{data.ServerData}\nPing Time: {Math.Floor(pingTimeMS)}ms";
                    }
                    else
                        message = $"Unexpected packet received\nPing Time: {Math.Floor(pingTimeMS)}ms";
                }
                else
                {
                    var pingTimeMS = DateTime.UtcNow.Subtract(pingTime).TotalMilliseconds;
                    message = $"Timed out\nPing Time: {Math.Floor(pingTimeMS)}ms";
                }
            }
            catch(Exception ex)
            {
                message = $"Error: {ex.Message}";
            }

            return message;
        }
        #endregion

        //Dispose Handlers
        ~VoiceCraftClient()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if(!IsDisposed)
            {
                if(disposing)
                {
                    CTS.Dispose();
                    Signalling.Dispose();
                    MCWSS.Dispose();
                    Voice.Dispose();
                    Participants.Clear();
                    IsConnected = false;

                    //Deregister Events
                    Signalling.OnAcceptPacketReceived -= SignallingAccept;
                    Signalling.OnBindedPacketReceived -= SignallingBinded;
                    Signalling.OnLoginPacketReceived -= SignallingLogin;
                    Signalling.OnLogoutPacketReceived -= SignallingLogout;
                    Signalling.OnDeafenPacketReceived -= SignallingDeafen;
                    Signalling.OnUndeafenPacketReceived -= SignallingUndeafen;
                    Signalling.OnMutePacketReceived -= SignallingMute;
                    Signalling.OnUnmutePacketReceived -= SignallingUnmute;
                    Voice.OnAcceptPacketReceived -= VoiceAccept;
                    Voice.OnServerAudioPacketReceived -= VoiceServerAudio;
                    MCWSS.OnConnect -= WebsocketConnected;
                    MCWSS.OnPlayerTravelled -= WebsocketPlayerTravelled;
                    MCWSS.OnDisconnect -= WebsocketDisconnected;
                    Signalling.OnSocketDisconnected -= SocketDisconnected;
                    Voice.OnSocketDisconnected -= SocketDisconnected;
                }
                IsDisposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        //private methods
        private static short[] BytesToShorts(byte[] input, int offset, int length)
        {
            short[] processedValues = new short[length / 2];
            for (int c = 0; c < processedValues.Length; c++)
            {
                processedValues[c] = (short)(input[c * 2 + offset] << 0);
                processedValues[c] += (short)(input[c * 2 + 1 + offset] << 8);
            }

            return processedValues;
        }
    }
}
