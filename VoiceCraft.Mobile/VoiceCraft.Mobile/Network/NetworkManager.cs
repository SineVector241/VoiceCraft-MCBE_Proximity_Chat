using Concentus.Structs;
using NAudio.Wave;
using System;
using System.Collections.Concurrent;
using System.Numerics;
using VoiceCraft.Mobile.Network.Codecs;
using VoiceCraft.Mobile.Network.Interfaces;
using VoiceCraft.Mobile.Network.Packets;
using VoiceCraft.Mobile.Network.Sockets;

namespace VoiceCraft.Mobile.Network
{
    public class NetworkManager : INetworkManager
    {
        public ConcurrentDictionary<uint, VoiceCraftParticipant> Participants { get; }

        public string IP { get; private set; }
        public int Port { get; private set; }
        public uint Key { get; private set; }
        public bool DirectionalHearing { get; }
        public bool ClientSidedPositioning { get; }
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
        public event INetworkManager.VoiceCraftParticipantJoined OnParticipantJoined;
        public event INetworkManager.VoiceCraftParticipantLeft OnParticipantLeft;

        //Constructor
        public NetworkManager(bool DirectionalHearing, bool ClientSidedPositioning, AudioCodecs Codec)
        {
            this.DirectionalHearing = DirectionalHearing;
            this.ClientSidedPositioning = ClientSidedPositioning;
            this.Codec = Codec;

            Participants = new ConcurrentDictionary<uint, VoiceCraftParticipant>();

            Signalling = new SignallingSocket(this);
            Voice = new VoiceSocket(this);

            OnConnect += NM_OnConnect;
            OnConnectError += NM_OnConnectError;
        }

        //Public Methods
        public void Connect(string IP, int Port)
        {
            this.IP = IP;
            this.Port = Port;
            Signalling.Connect();
        }

        public void Disconnect(string reason = null)
        {
            Signalling.Disconnect();
            Voice.Disconnect();
            if(ClientSidedPositioning || Websocket != null) Websocket?.Disconnect();
        }

        public void SendAudio(byte[] Data)
        {
            VoicePacket packet = new VoicePacket(); //Audio packet stuff here.
            Voice.SendPacket(packet);
        }

        public void SendAudio(byte[] Data, Vector3 Position)
        {
            VoicePacket packet = new VoicePacket() { }; //Audio packet stuff here.
            Voice.SendPacket(packet);
        }

        //Private Methods
        private void NM_OnConnect(SocketTypes SocketType, int SampleRate)
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
            }
            catch(Exception ex)
            {
                OnConnectError?.Invoke(SocketTypes.NetworkManager, ex.Message);
            }
        }

        private void NM_OnConnectError(SocketTypes SocketType, string reason)
        {
            Disconnect();
        }
    }
}
