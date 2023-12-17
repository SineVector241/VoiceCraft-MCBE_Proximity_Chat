using Newtonsoft.Json;
using VoiceCraft.Core.Packets.Interfaces;
using VoiceCraft.Core.Packets.MCComm;

namespace VoiceCraft.Core.Packets
{
    public class MCCommPacket : IMCCommPacket
    {
        public MCCommPacketTypes PacketType { get; set; }
        public object PacketData { get; set; } = new Null();

        public MCCommPacket()
        {
            PacketType = MCCommPacketTypes.Null;
        }

        public MCCommPacket(string data)
        {
            var jsonData = JsonConvert.DeserializeObject<MCCommPacket>(data);
            if (jsonData != null)
            {
                PacketType = jsonData.PacketType;
                switch(PacketType)
                {
                    case MCCommPacketTypes.Login: PacketData = (Login)jsonData.PacketData;
                        break;
                    case MCCommPacketTypes.Bind: PacketData = (Bind)jsonData.PacketData;
                        break;
                    case MCCommPacketTypes.Update: PacketData = (Update)jsonData.PacketData;
                        break;
                    case MCCommPacketTypes.UpdateSettings: PacketData = (UpdateSettings)jsonData.PacketData;
                        break;
                    case MCCommPacketTypes.GetSettings: PacketData = (GetSettings)jsonData.PacketData;
                        break;
                    case MCCommPacketTypes.RemoveParticipant: PacketData = (RemoveParticipant)jsonData.PacketData;
                        break;
                }
            }
            //Else do nothing
        }
    }

    public enum MCCommPacketTypes
    {
        Login,
        Bind,
        Update,
        UpdateSettings,
        GetSettings,
        RemoveParticipant,
        Null
    }
}
