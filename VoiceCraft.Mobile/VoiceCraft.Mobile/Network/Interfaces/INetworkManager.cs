using NAudio.Wave;
using System.Collections.Concurrent;
using VoiceCraft.Mobile.Network.Codecs;
using VoiceCraft.Mobile.Network.Sockets;

namespace VoiceCraft.Mobile.Network.Interfaces
{
    public interface INetworkManager
    {
        //Variables
        public ConcurrentDictionary<ushort, VoiceCraftParticipant> Participants { get; }

        public string IP { get; }
        public ushort Port { get; }
        public ushort Key { get; }
        public ushort VoicePort { get; set; }
        public bool DirectionalHearing { get; }
        public bool ClientSidedPositioning { get; }
        public int AudioFrameSizeMS { get; }
        public AudioCodecs Codec { get; }
        public WaveFormat RecordFormat { get; }
        public WaveFormat PlayFormat { get; }

        //Events
        public delegate void SocketConnect(SocketTypes SocketType, int SampleRate, ushort Key);
        public delegate void SocketConnectError(SocketTypes SocketType, string reason);
        public delegate void SocketDisconnect(string reason = null);
        public delegate void Binded(string Username);
        public delegate void VoiceCraftParticipantJoined(ushort Key, VoiceCraftParticipant Participant);
        public delegate void VoiceCraftParticipantLeft(ushort Key, VoiceCraftParticipant Participant);

        public event SocketConnect OnConnect;
        public event SocketConnectError OnConnectError;
        public event SocketDisconnect OnDisconnect;
        public event Binded OnBinded;
        public event VoiceCraftParticipantJoined OnParticipantJoined;
        public event VoiceCraftParticipantLeft OnParticipantLeft;

        /// <summary>
        /// Begins connection to a VoiceCraft server.
        /// </summary>
        /// <param name="IP">Address</param>
        /// <param name="Port">Port</param>
        public void Connect(string IP, ushort Port);

        /// <summary>
        /// Disconnects all connected sockets.
        /// </summary>
        /// <param name="reason">Reason for disconnection to be passed into OnDisconnect event.</param>
        public void Disconnect(string reason = null);

        /// <summary>
        /// Sends audio.
        /// </summary>
        /// <param name="Data">Audio data. MUST BE PCM DATA!!</param>
        public void SendAudio(byte[] Data, int BytesRecorded, uint AudioPacketCount);

        //Event Firing
        /// <summary>
        /// Fires the OnConnect event.
        /// </summary>
         
        public void PerformConnect(SocketTypes SocketType, int SampleRate, ushort Key);

        /// <summary>
        /// Fires the OnConnectError event.
        /// </summary>
        public void PerformConnectError(SocketTypes SocketType, string reason);

        /// <summary>
        /// Fires the OnParticipantJoined event.
        /// </summary>
        public void PerformParticipantJoined(ushort Key, VoiceCraftParticipant Participant);

        /// <summary>
        /// Fires the OnParticipantLeft event.
        /// </summary>
        public void PerformParticipantLeft(ushort Key);

        /// <summary>
        /// Fires the OnBinded event.
        /// </summary>
        public void PerformBinded(string Username);
    }
}
