using VoiceCraft.Mobile.Network.Sockets;
using static VoiceCraft.Mobile.Network.Interfaces.INetworkManager;

namespace VoiceCraft.Mobile.Network.Interfaces
{
    public interface INetwork
    {
        public INetworkManager Manager { get; }

        /// <summary>
        /// Connects to the socket.
        /// </summary>
        public void Connect();

        /// <summary>
        /// Disconnects from the socket and disposes with optional reason.
        /// </summary>
        public void Disconnect();

        /// <summary>
        /// Sends a packet on the socket.
        /// </summary>
        /// <param name="PacketStream"></param>
        public void SendPacket(byte[] PacketStream);
    }
}
