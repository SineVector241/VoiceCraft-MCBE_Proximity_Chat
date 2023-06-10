using System.Net.Sockets;
using VoiceCraft.Mobile.Network.Interfaces;

namespace VoiceCraft.Mobile.Network.Sockets
{
    public class VoiceSocket : INetwork
    {
        public INetworkManager Manager { get; }
        public UdpClient UDPSocket { get; set; }
        public VoiceSocket(INetworkManager Manager) => this.Manager = Manager;

        public void Connect()
        {
            
        }

        public void Disconnect()
        {
        }

        public void SendPacket(byte[] PacketStream)
        {

        }
    }
}
