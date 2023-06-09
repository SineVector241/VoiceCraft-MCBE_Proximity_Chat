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
        public void Disconnect(string Reason = null);

        /// <summary>
        /// Sends a packet on the socket.
        /// </summary>
        /// <param name="Packet"></param>
        public void SendPacket(object Packet);
    }
}
