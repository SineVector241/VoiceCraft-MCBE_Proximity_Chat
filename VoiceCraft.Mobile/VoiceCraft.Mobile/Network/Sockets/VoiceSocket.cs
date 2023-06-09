using VoiceCraft.Mobile.Network.Interfaces;

namespace VoiceCraft.Mobile.Network.Sockets
{
    public class VoiceSocket : INetwork
    {
        public INetworkManager Manager { get; }

        public VoiceSocket(INetworkManager Manager) => this.Manager = Manager;

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
