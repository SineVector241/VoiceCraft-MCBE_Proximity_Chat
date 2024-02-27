using NAudio.Wave;
using System.Threading.Tasks;
using VoiceCraft.Core.Packets;
using VoiceCraft.Core.Sockets;
using System;

namespace VoiceCraft.Client
{
    public class VoiceCraftClient
    {
        public const string Version = "v1.0.3";

        public ConnectionState ConnectionState { get; private set; }
        public WaveFormat AudioFormat { get; }
        public Signalling Signalling { get; }
        public Voice Voice { get; }
        public string? IP { get; private set; }

        #region Events
        public delegate void SignallingConnected();
        public delegate void VoiceConnected();
        public delegate void Disconnected(string? reason = null);

        public event SignallingConnected? OnSignallingConnected;
        public event VoiceConnected? OnVoiceConnected;
        public event Disconnected? OnDisconnected;
        #endregion

        public VoiceCraftClient(WaveFormat audioFormat)
        {
            AudioFormat = audioFormat;
            Signalling = new Signalling();
            Voice = new Voice();

            //Event Registry
            Signalling.OnConnected += Signalling_Connected;

            Voice.OnConnected += Voice_Connected;
        }

        public async Task Connect(string IP, int Port, ushort PreferredKey, PositioningTypes positioningType)
        {
            if (ConnectionState != ConnectionState.Disconnected) throw new Exception("You must disconnect before connecting!");

            this.IP = IP;
            await Signalling.Connect(IP, Port, PreferredKey, positioningType, Version);
        }

        public void Disconnect(string? Reason = null)
        {
            if (ConnectionState == ConnectionState.Disconnected) return; //Already Disconnected.
            
            OnDisconnected?.Invoke(Reason); //Should not happen.
            Signalling.Disconnect();
            Voice.Disconnect();
        }

        #region Event Methods
        private void Signalling_Connected(ushort port, ushort key = 0)
        {
            OnSignallingConnected?.Invoke();
            if (!string.IsNullOrWhiteSpace(IP))
            {
                _ = Voice.Connect(IP, port, key);
            }
            else
            {
                Disconnect("IP WAS SOMEHOW EMPTY!");
            }
        }

        private void Voice_Connected()
        {
            OnVoiceConnected?.Invoke();
        }
        #endregion
    }

    public enum ConnectionState
    {
        Connecting,
        Connected,
        Disconnected
    }
}
