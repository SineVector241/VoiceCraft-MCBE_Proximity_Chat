using NAudio.Wave;
using System.Collections.Concurrent;
using System.Numerics;
using VoiceCraft.Mobile.Network.Codecs;
using VoiceCraft.Mobile.Network.Sockets;

namespace VoiceCraft.Mobile.Network.Interfaces
{
    public interface INetworkManager
    {
        //Variables
        public ConcurrentDictionary<uint, VoiceCraftParticipant> Participants { get; }

        public string IP { get; }
        public int Port { get; }
        public uint Key { get; }
        public bool DirectionalHearing { get; }
        public bool ClientSidedPositioning { get; }
        public AudioCodecs Codec { get; }
        public WaveFormat RecordFormat { get; }
        public WaveFormat PlayFormat { get; }

        //Events
        public delegate void SocketConnect(SocketTypes SocketType, int SampleRate);
        public delegate void SocketConnectError(SocketTypes SocketType, string reason);
        public delegate void SocketDisconnect(SocketTypes SocketType, string reason = null);
        public delegate void VoiceCraftParticipantJoined(VoiceCraftParticipant Participant);
        public delegate void VoiceCraftParticipantLeft(VoiceCraftParticipant Participant);

        public event SocketConnect OnConnect;
        public event SocketConnectError OnConnectError;
        public event SocketDisconnect OnDisconnect;
        public event VoiceCraftParticipantJoined OnParticipantJoined;
        public event VoiceCraftParticipantLeft OnParticipantLeft;

        /// <summary>
        /// Begins connection to a VoiceCraft server.
        /// </summary>
        /// <param name="IP">Address</param>
        /// <param name="Port">Port</param>
        public void Connect(string IP, int Port);

        /// <summary>
        /// Disconnects all connected sockets.
        /// </summary>
        /// <param name="reason">Reason for disconnection to be passed into OnDisconnect event.</param>
        public void Disconnect(string reason = null);

        /// <summary>
        /// Sends audio for server sided voice connection.
        /// </summary>
        /// <param name="Data">Audio data. MUST BE PCM DATA!!</param>
        public void SendAudio(byte[] Data);

        /// <summary>
        /// Sends audio for client sided voice connection.
        /// </summary>
        /// <param name="Data">Audio data. MUST BE PCM DATA!!</param>
        /// <param name="Position">Position of the client ingame.</param>
        public void SendAudio(byte[] Data, Vector3 Position);
    }
}
