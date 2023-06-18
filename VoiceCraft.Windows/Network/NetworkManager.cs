using Concentus.Structs;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using VoiceCraft.Windows.Network.Packets;
using VoiceCraft.Windows.Network.Sockets;

namespace VoiceCraft.Windows.Network
{
    public class NetworkManager
    {
        //Constants
        public const int SampleRate = 48000;

        //Variables
        public readonly string IP = string.Empty;
        public readonly int Port;
        public readonly bool ClientSided;
        public readonly bool DirectionalHearing;
        public readonly ConcurrentDictionary<ushort, VoiceCraftParticipant> Participants;
        public bool Disconnecting { get; private set; }
        public ushort Key { get; private set; }
        public int VoicePort { get; private set; }
        public uint PacketCount { get; private set; }

        //Audio Variables
        public readonly int RecordLengthMS;
        public readonly WaveFormat RecordFormat = new WaveFormat(SampleRate, 1);
        public readonly WaveFormat PlaybackFormat = WaveFormat.CreateIeeeFloatWaveFormat(SampleRate, 2);
        public readonly MixingSampleProvider Mixer;

        //Codec Encoder
        private readonly OpusEncoder Encoder;

        //Sockets
        public readonly SignallingSocket Signalling;
        public readonly VoiceSocket Voice;
        public readonly WebsocketSocket Websocket;

        //Events
        public delegate void SignallingConnect(ushort Key, int VoicePort);
        public delegate void VoiceConnect();
        public delegate void WebsocketConnect();
        public delegate void WebsocketDisconnect();
        public delegate void Bind(string Name);
        public delegate void ConnectError(string Reason);
        public delegate void ParticipantJoined(ushort Key, VoiceCraftParticipant Participant);
        public delegate void ParticipantLeft(ushort Key, VoiceCraftParticipant? Participant);
        public delegate void Disconnect(string? Reason = null);

        public event SignallingConnect? OnSignallingConnect;
        public event VoiceConnect? OnVoiceConnect;
        public event WebsocketConnect? OnWebsocketConnect;
        public event WebsocketDisconnect? OnWebsocketDisconnect;
        public event Bind? OnBind;
        public event ParticipantJoined? OnParticipantJoined;
        public event ParticipantLeft? OnParticipantLeft;
        public event Disconnect? OnDisconnect;

        public NetworkManager(string IP, int Port, ushort Key, bool ClientSided = false, bool DirectionalHearing = false, int RecordLengthMS = 40)
        {
            //Variable Assignments
            this.IP = IP;
            this.Port = Port;
            this.Key = Key;
            this.ClientSided = ClientSided;
            this.DirectionalHearing = DirectionalHearing;
            Participants = new ConcurrentDictionary<ushort, VoiceCraftParticipant>();

            Encoder = new OpusEncoder(SampleRate, 1, Concentus.Enums.OpusApplication.OPUS_APPLICATION_VOIP)
            {
                Bitrate = 32000,
                Complexity = 5,
                UseVBR = true,
                PacketLossPercent = 40
            };

            //Audio Variable Assignments
            this.RecordLengthMS = RecordLengthMS;
            Mixer = new MixingSampleProvider(PlaybackFormat) { ReadFully = true };

            //Socket Assignements
            Signalling = new SignallingSocket(this);
            Voice = new VoiceSocket(this);
            Websocket = new WebsocketSocket(this);

            //Event Listening
            Signalling.OnConnect += SC_OnConnect;
            Signalling.OnBind += SC_OnBind;
            Signalling.OnParticipantJoined += SC_OnParticipantJoined;
            Signalling.OnParticipantLeft += SC_OnParticipantLeft;

            Voice.OnConnect += VC_OnConnect;

            Websocket.OnConnect += WS_OnConnect;
            Websocket.OnDisconnect += WS_OnDisconnect;
        }

        public void StartConnect()
        {
            Signalling.StartConnect();
        }

        public void StartDisconnect(string? Reason = null, bool SendDisconnectPacket = false)
        {
            if (Disconnecting) return; //We've already disconnected.

            if(SendDisconnectPacket)
            {
                Signalling.SendPacket(new SignallingPacket() { PacketIdentifier = SignallingPacketIdentifiers.Logout }.GetPacketDataStream());
            }

            Disconnecting = true;
            Signalling.StartDisconnect();
            Voice.StartDisconnect();
            if(ClientSided) Websocket.StartDisconnect();
            OnDisconnect?.Invoke(Reason);
        }

        public void SendAudio(byte[] Data, int BytesRecorded)
        {
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
                PacketIdentifier = VoicePacketIdentifier.Audio,
                PacketAudio = audioTrimmed, //Sends trimmed bytes to save packet size.
                PacketCount = PacketCount
            }; //Audio packet stuff here.
            Voice.SendPacket(packet.GetPacketDataStream());
        }

        public void ResetPacketCounter()
        {
            PacketCount = 0;
        }

        public static async Task<string> InfoPingAsync(string IP, ushort Port)
        {
            var UDPSocket = new UdpClient();
            try
            {
                //Connect and send.
                UDPSocket.Connect(IP, Port);
                var pingPacket = new SignallingPacket() { PacketIdentifier = SignallingPacketIdentifiers.InfoPing, PacketVersion = App.Version }.GetPacketDataStream();
                await UDPSocket.SendAsync(pingPacket, pingPacket.Length);
                var pingTime = DateTime.UtcNow;

                //Receive and parse...
                var response = UDPSocket.ReceiveAsync();
                var timeout = Task.Delay(5000);
                var tasks = await Task.WhenAny(response, timeout);

                var pingTimeMS = DateTime.UtcNow.Subtract(pingTime).TotalMilliseconds;

                if (tasks == response)
                {
                    var packet = new SignallingPacket(response.Result.Buffer);
                    UDPSocket.Close();
                    UDPSocket.Dispose();
                    UDPSocket = null;
                    if (packet.PacketIdentifier != SignallingPacketIdentifiers.Deny)
                        return $"{packet.PacketMetadata}\nPing Time: {Math.Floor(pingTimeMS)}ms";

                    return $"Banned from server...\nPing Time: {Math.Floor(pingTimeMS)}ms";
                }
                else
                {
                    UDPSocket.Close();
                    UDPSocket.Dispose();
                    UDPSocket = null;
                    return $"Error. Timed Out...\nPing Time: {Math.Floor(pingTimeMS)}ms";
                }

            }
            catch (Exception ex)
            {
                //If errored. Disconnect and dispose.
                if (UDPSocket != null && UDPSocket.Client.Connected)
                {
                    UDPSocket.Close();
                    UDPSocket.Dispose();
                }
                return ex.Message;
            }
        }

        //Signalling Client Events
        private void SC_OnConnect(ushort Key, int VoicePort)
        {
            this.Key = Key;
            this.VoicePort = VoicePort;
            Voice.StartConnect();

            OnSignallingConnect?.Invoke(Key, VoicePort);
        }

        private void SC_OnBind(string Name)
        {
            OnBind?.Invoke(Name);
        }

        private void SC_OnParticipantJoined(ushort Key, VoiceCraftParticipant Participant)
        {
            var result = Participants.TryAdd(Key, Participant);
            Mixer.AddMixerInput(Participant.AudioProvider);
            if (result)
                OnParticipantJoined?.Invoke(Key, Participant);
        }

        private void SC_OnParticipantLeft(ushort Key)
        {
            var result = Participants.TryRemove(Key, out VoiceCraftParticipant? Participant);
            Mixer.RemoveMixerInput(Participant?.AudioProvider);
            if (result)
                OnParticipantLeft?.Invoke(Key, Participant);
        }

        //Voice Client Events
        private void VC_OnConnect()
        {
            if (ClientSided)
            {
                Websocket.StartConnect();
            }

            OnVoiceConnect?.Invoke();
        }

        //Websocket Client Events
        private void WS_OnConnect(string Username)
        {
            OnWebsocketConnect?.Invoke();
        }

        private void WS_OnDisconnect()
        {
            OnWebsocketDisconnect?.Invoke();
        }

        //Private Methods
        private static short[] BytesToShorts(byte[] input, int offset, int length)
        {
            short[] processedValues = new short[length / 2];
            for (int c = 0; c < processedValues.Length; c++)
            {
                processedValues[c] = (short)(((int)input[(c * 2) + offset]) << 0);
                processedValues[c] += (short)(((int)input[(c * 2) + 1 + offset]) << 8);
            }

            return processedValues;
        }
    }
}
