using System.Numerics;

namespace VoiceCraft.Mobile.Network.Packets
{
    public enum VoicePacketIdentifier
    {
        Login,
        Logout,
        Accept,
        Deny,
        Audio,
        UpdatePosition,
        Error,
        Null
    }

    public class VoicePacket
    {
        private VoicePacketIdentifier Identifier; //Data containing the packet identifier.
        private uint Count; //Data containing packet count to detect packet loss.
        private ushort Key; //Data containing the key of a participant.
        private Vector3 Position; //Data containing audio source assuming client audio handling is at 0,0,0 rotation 0.
        private byte[] Audio; //Data containing encoded audio data.
        private string Version; //Data containing packet version.
    }
}
