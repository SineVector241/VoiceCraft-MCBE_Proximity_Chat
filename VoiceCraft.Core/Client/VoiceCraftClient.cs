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
using System.IO;
using System.Diagnostics;

namespace VoiceCraft.Core.Client
{
    public class VoiceCraftClient
    {
        //Constants
        public const int SampleRate = 48000;

        //Variables
        private bool VoiceConnected = false;
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
            Signalling = new SignallingSocket(CTS.Token);
            Voice = new VoiceSocket(CTS.Token);
            MCWSS = new MCWSSSocket(MCWSSPort);

            //Event Registration in login order.
            Signalling.OnAcceptPacketReceived += SignallingAccept;
            Signalling.OnDenyPacketReceived += SignallingDeny;
            Voice.OnAcceptPacketReceived += VoiceAccept;
            Voice.OnDenyPacketReceived += VoiceDeny;
            Signalling.OnBindedPacketReceived += SignallingBinded;
            MCWSS.OnConnect += WebsocketConnected;
            Signalling.OnLoginPacketReceived += SignallingLogin;
            Voice.OnServerAudioPacketReceived += VoiceServerAudio;
            Signalling.OnLogoutPacketReceived += SignallingLogout;
            MCWSS.OnDisconnect += WebsocketDisconnected;
            MCWSS.OnPlayerTravelled += WebsocketPlayerTravelled;

            Signalling.OnDeafenPacketReceived += SignallingDeafen;
            Signalling.OnUndeafenPacketReceived += SignallingUndeafen;
            Signalling.OnMutePacketReceived += SignallingMute;
            Signalling.OnUnmutePacketReceived += SignallingUnmute;

            Signalling.OnSocketDisconnected += SocketDisconnected;
        }

        private void SocketDisconnected(string reason)
        {
            Disconnect();
            OnDisconnected?.Invoke(reason);
        }

        //Signalling Event Methods
        #region Signalling
        private void SignallingAccept(Packets.Signalling.Accept packet)
        {
            LoginKey = packet.LoginKey;
            Voice.Connect(IP, packet.VoicePort);
            Voice.SendPacketAsync(new VoicePacket()
            {
                PacketType = VoicePacketTypes.Login,
                PacketData = new Packets.Voice.Login()
                {
                    LoginKey = LoginKey
                }
            });
            //Packets could be dropped. So we need to loop. This will be implemented later.

            _ = Task.Run(async () =>
            {
                await Task.Delay(5000);
                if(!VoiceConnected && !CTS.IsCancellationRequested)
                {
                    Disconnect();
                    OnDisconnected?.Invoke("Voice connection timed out");
                }
            });
        }

        private void SignallingDeny(Packets.Signalling.Deny packet)
        {
            Disconnect();
            OnDisconnected?.Invoke(packet.Reason);
        }

        private void SignallingBinded(Packets.Signalling.Binded packet)
        {
            if (!VoiceConnected) return;

            OnBinded?.Invoke(packet.Name);
        }

        private void SignallingLogin(Packets.Signalling.Login packet)
        {
            if (!Participants.ContainsKey(packet.LoginKey) && VoiceConnected)
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
            if (!VoiceConnected) return;

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
            if (!VoiceConnected) return;

            Participants.TryGetValue(packet.LoginKey, out var participant);
            if (participant != null)
            {
                participant.Deafened = true;
                OnParticipantUpdated?.Invoke(participant, packet.LoginKey);
            }
        }

        private void SignallingUndeafen(Packets.Signalling.Undeafen packet)
        {
            if (!VoiceConnected) return;

            Participants.TryGetValue(packet.LoginKey, out var participant);
            if (participant != null)
            {
                participant.Deafened = false;
                OnParticipantUpdated?.Invoke(participant, packet.LoginKey);
            }
        }

        private void SignallingMute(Packets.Signalling.Mute packet)
        {
            if (!VoiceConnected) return;

            Participants.TryGetValue(packet.LoginKey, out var participant);
            if (participant != null)
            {
                participant.Muted = true;
                OnParticipantUpdated?.Invoke(participant, packet.LoginKey);
            }
        }

        private void SignallingUnmute(Packets.Signalling.Unmute packet)
        {
            if (!VoiceConnected) return;

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
            VoiceConnected = true;
            OnConnected?.Invoke();
            if(PositioningType == PositioningTypes.ClientSided) MCWSS.Start();
        }

        private void VoiceDeny(Packets.Voice.Deny packet)
        {
            Disconnect();
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
        #endregion
        //Websocket Event Methods
        #region MCWSS
        private void WebsocketConnected(string Username)
        {
            Signalling.SendPacketAsync(new SignallingPacket()
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
            Voice.SendPacketAsync(new VoicePacket()
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
            Signalling.SendPacketAsync(new SignallingPacket()
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
            VoiceConnected = false;
            this.IP = IP;
            this.Port = Port;

            if (CTS.IsCancellationRequested)
            {
                CTS.Dispose();
                CTS = new CancellationTokenSource();
                Signalling = new SignallingSocket(CTS.Token);
                Voice = new VoiceSocket(CTS.Token);
                MCWSS = new MCWSSSocket(MCWSSPort);
            }
            Signalling.ConnectAsync(IP, Port, LoginKey, PositioningType, Version);
        }

        public void Disconnect()
        {
            if (!CTS.IsCancellationRequested)
            {
                CTS.Cancel();
                VoiceConnected = false;
                Signalling.Disconnect();
                Voice.Disconnect();
                if (PositioningType == PositioningTypes.ClientSided) MCWSS.Stop();
            }
        }

        public void SendAudio(byte[] Data, int BytesRecorded)
        {
            if (IsDeafened || IsMuted) return;

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
            Voice.SendPacketAsync(packet);
        }

        public void SetMute(bool Muted)
        {
            IsMuted = Muted;
            if(IsMuted)
            {
                Signalling.SendPacketAsync(new SignallingPacket()
                {
                    PacketType = SignallingPacketTypes.Mute,
                    PacketData = new Packets.Signalling.Mute()
                });
            }
            else
            {
                Signalling.SendPacketAsync(new SignallingPacket()
                {
                    PacketType = SignallingPacketTypes.Unmute,
                    PacketData = new Packets.Signalling.Unmute()
                });
            }
        }

        public void SetDeafen(bool Deafened)
        {
            IsDeafened = Deafened;
            if (IsDeafened)
            {
                Signalling.SendPacketAsync(new SignallingPacket()
                {
                    PacketType = SignallingPacketTypes.Deafen,
                    PacketData = new Packets.Signalling.Deafen()
                });
            }
            else
            {
                Signalling.SendPacketAsync(new SignallingPacket()
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
    }
}
