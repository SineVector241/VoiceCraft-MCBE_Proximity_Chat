using VoiceCraft.Mobile.Interfaces;

namespace VoiceCraft.Mobile.Network.Sockets
{
    public class VoiceSocket : INetwork
    {
        public bool IsClientSided { get; }

        public void Connect(INetworkManager networkManager)
        {
            throw new System.NotImplementedException();
        }

        public void Disconnect()
        {
            throw new System.NotImplementedException();
        }

        public void Disconnect(string reason)
        {
            throw new System.NotImplementedException();
        }
    }
}
