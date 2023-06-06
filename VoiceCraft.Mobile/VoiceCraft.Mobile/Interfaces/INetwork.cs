namespace VoiceCraft.Mobile.Interfaces
{
    public interface INetwork
    {
        public bool IsClientSided { get; }

        public void Connect(INetworkManager networkManager);
        public void Disconnect();
        public void Disconnect(string reason);
    }
}
