using VoiceCraft.Core.Packets.Interfaces;

namespace VoiceCraft.Core.Packets.MCComm
{
    public class Login : IMCCommPacketData
    {
        public string LoginKey { get; set; } = string.Empty;

        public static MCCommPacket Create(string loginKey)
        {
            return new MCCommPacket()
            {
                PacketType = MCCommPacketTypes.Login,
                PacketData = new Login()
                {
                    LoginKey = loginKey
                }
            };
        }
    }
}
