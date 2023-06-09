using VoiceCraft.Mobile.Network.Interfaces;

namespace VoiceCraft.Mobile.Network.Sockets
{
    public class SignallingSocket : INetwork
    {
        public INetworkManager Manager { get; }

        public SignallingSocket(INetworkManager Manager) => this.Manager = Manager;

        public void Connect(INetworkManager NetworkManager)
        {
            throw new System.NotImplementedException();
        }

        public void Connect()
        {
            throw new System.NotImplementedException();
        }

        public void Disconnect(string Reason = null)
        {
            throw new System.NotImplementedException();
        }

        public void SendPacket(object Packet)
        {
            throw new System.NotImplementedException();
        }
    }
}
