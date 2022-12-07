using System;
using System.Net;
using VoiceCraftProximityChat.Utils;

namespace VoiceCraftProximityChat.Models
{
    public interface IUdpClient
    {
        public void Connect(IPAddress IPAddress, int Port);
        public void Login(string Key, Action<bool> func);
        public void SendPacket(Packet packet);
        public void RecievePacket(IAsyncResult asyncResult);
        public void HandlePacket(Packet packetData);
        public void Dispose();
    }
}
