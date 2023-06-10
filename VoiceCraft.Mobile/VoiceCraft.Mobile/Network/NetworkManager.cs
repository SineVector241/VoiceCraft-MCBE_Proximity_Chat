using Concentus.Structs;
using NAudio.Wave;
using System;
using System.Collections.Concurrent;
using System.Linq;
using VoiceCraft.Mobile.Network.Codecs;
using VoiceCraft.Mobile.Network.Interfaces;
using VoiceCraft.Mobile.Network.Packets;
using VoiceCraft.Mobile.Network.Sockets;

namespace VoiceCraft.Mobile.Network
{
    public class NetworkManager : INetworkManager
    {
        public ConcurrentDictionary<ushort, VoiceCraftParticipant> Participants { get; }

        public string IP { get; private set; }
        public ushort Port { get; private set; }
        public ushort Key { get; private set; }
        public ushort VoicePort { get; set; }
        public bool DirectionalHearing { get; }
        public bool ClientSidedPositioning { get; }
        public int AudioFrameSizeMS { get; }
        public AudioCodecs Codec { get; }
#nullable enable
        public WaveFormat? RecordFormat { get; private set; }
        public WaveFormat? PlayFormat { get; private set; }

        public INetwork Signalling { get; }
        public INetwork Voice { get; }
        public INetwork? Websocket { get; }

        public G722ChatCodec? G722Codec { get; private set; }
        public OpusEncoder? OpusEncoder { get; private set; }
#nullable disable

        public event INetworkManager.SocketConnect OnConnect;
        public event INetworkManager.SocketConnectError OnConnectError;
        public event INetworkManager.SocketDisconnect OnDisconnect;
        public event INetworkManager.Binded OnBinded;
        public event INetworkManager.VoiceCraftParticipantJoined OnParticipantJoined;
        public event INetworkManager.VoiceCraftParticipantLeft OnParticipantLeft;

        //Constructor
        public NetworkManager(bool DirectionalHearing, bool ClientSidedPositioning, AudioCodecs Codec, int AudioFrameSizeMS)
        {
            //Setup the readonly variables.
            this.DirectionalHearing = DirectionalHearing;
            this.ClientSidedPositioning = ClientSidedPositioning;
            this.Codec = Codec;
            this.AudioFrameSizeMS = AudioFrameSizeMS;

            //Setup participants list.
            Participants = new ConcurrentDictionary<ushort, VoiceCraftParticipant>();

            //Setup the sockets
            Signalling = new SignallingSocket(this);
            Voice = new VoiceSocket(this);
        }

        //Public Methods
        public void Connect(string IP, ushort Port)
        {
            //Setup IP and Port variables and start connection protocol.
            this.IP = IP;
            this.Port = Port;
            Signalling.Connect();
        }

        public void Disconnect(string reason = null)
        {
            //Disconnect all sockets.
            Signalling.Disconnect();
            Voice.Disconnect();
            if(ClientSidedPositioning || Websocket != null) Websocket?.Disconnect(); //If client sided positioning then disconnect websocket.

            OnDisconnect?.Invoke(reason);
        }

        public void SendAudio(byte[] Data, int BytesRecorded, uint AudioPacketCount)
        {
            byte[] audioEncodeBuffer = new byte[1000];
            byte[] audioTrimmed = new byte[0];
            switch(Codec)
            {
                //If opus. Encode on opus level.
                case AudioCodecs.Opus:
                    if (OpusEncoder == null)
                        return;

                    short[] pcm = BytesToShorts(Data, 0, BytesRecorded);
                    var encodedBytes = OpusEncoder.Encode(pcm, 0, pcm.Length, audioEncodeBuffer, 0, audioEncodeBuffer.Length);
                    audioTrimmed = audioEncodeBuffer.SkipLast(1000 - encodedBytes).ToArray();
                    break;
                //If G722. Encode on G722 level.
                case AudioCodecs.G722:
                    if (G722Codec == null)
                        return;

                    audioTrimmed = G722Codec.Encode(Data, 0, BytesRecorded);
                    break;
            }
            //Packet creation.
            VoicePacket packet = new VoicePacket() {
                PacketIdentifier = VoicePacketIdentifier.Audio,
                PacketAudio = audioTrimmed, //Sends trimmed bytes to save packet size.
                PacketCount = AudioPacketCount
            }; //Audio packet stuff here.
            Voice.SendPacket(packet.GetPacketDataStream());
        }

        public void PerformConnect(SocketTypes SocketType, int SampleRate, ushort Key)
        {
            try
            {
                switch (SocketType)
                {
                    case SocketTypes.Signalling:
                        RecordFormat = new WaveFormat(SampleRate, 1);
                        PlayFormat = WaveFormat.CreateIeeeFloatWaveFormat(SampleRate, 2);

                        switch (Codec)
                        {
                            case AudioCodecs.Opus:
                                OpusEncoder = new OpusEncoder(SampleRate, 1, Concentus.Enums.OpusApplication.OPUS_APPLICATION_VOIP);
                                OpusEncoder.Bitrate = 32000;
                                OpusEncoder.Complexity = 5;
                                OpusEncoder.UseVBR = true;
                                OpusEncoder.PacketLossPercent = 40;
                                break;
                            case AudioCodecs.G722:
                                G722Codec = new G722ChatCodec();
                                break;
                        }
                        Voice.Connect();
                        break;
                    case SocketTypes.Voice:
                        if (ClientSidedPositioning) Websocket.Connect();
                        break;
                }

                OnConnect?.Invoke(SocketType, SampleRate, Key);
            }
            catch (Exception ex)
            {
                OnConnectError?.Invoke(SocketTypes.NetworkManager, ex.Message);
            }
        }

        public void PerformConnectError(SocketTypes SocketType, string reason)
        {
            Disconnect();
            OnConnectError?.Invoke(SocketType, reason);
        }

        public void PerformParticipantJoined(ushort Key, VoiceCraftParticipant Participant)
        {
            Participants.TryAdd(Key, Participant);
            OnParticipantJoined?.Invoke(Key, Participant);
        }

        public void PerformParticipantLeft(ushort Key)
        {
            VoiceCraftParticipant participant;
            Participants.TryRemove(Key, out participant);
            OnParticipantLeft?.Invoke(Key, participant);
        }

        public void PerformBinded(string Username)
        {
            OnBinded?.Invoke(Username);
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
