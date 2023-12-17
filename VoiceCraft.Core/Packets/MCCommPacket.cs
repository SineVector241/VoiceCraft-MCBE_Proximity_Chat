using Newtonsoft.Json;
using VoiceCraft.Core.Packets.Interfaces;
using VoiceCraft.Core.Packets.MCComm;

namespace VoiceCraft.Core.Packets
{
    public class MCCommPacket : IMCCommPacket
    {
        public MCCommPacketTypes PacketType { get; set; } = MCCommPacketTypes.Null;
        public object PacketData { get; set; }

        public MCCommPacket()
        {
            PacketData = new Null();
        }

        public MCCommPacket(string data)
        {
            PacketData = new Null();

            var jsonData = JsonConvert.DeserializeObject<MCPacket>(data);
            if (jsonData != null)
            {
                switch (jsonData.PacketType)
                {
                    case MCCommPacketTypes.Login:
                        PacketData = (Login)jsonData.PacketData;
                        break;
                    case MCCommPacketTypes.Accept:
                        PacketData = (Accept)jsonData.PacketData;
                        break;
                    case MCCommPacketTypes.Deny:
                        PacketData = (Deny)jsonData.PacketData;
                        break;
                    case MCCommPacketTypes.Bind:
                        PacketData = (Bind)jsonData.PacketData;
                        break;
                    case MCCommPacketTypes.Update:
                        PacketData = (Update)jsonData.PacketData;
                        break;
                    case MCCommPacketTypes.UpdateSettings:
                        PacketData = (UpdateSettings)jsonData.PacketData;
                        break;
                    case MCCommPacketTypes.GetSettings:
                        PacketData = (GetSettings)jsonData.PacketData;
                        break;
                    case MCCommPacketTypes.RemoveParticipant:
                        PacketData = (RemoveParticipant)jsonData.PacketData;
                        break;
                }
            }
            else
                throw new JsonReaderException("Invalid content!");
        }

        public string GetPacketString()
        {
            return JsonConvert.SerializeObject(this);
        }

        private class MCPacket
        {
            public MCCommPacketTypes PacketType { get; set; } = MCCommPacketTypes.Null;
            public object PacketData { get; set; } = new Null();
        }
    }

    public enum MCCommPacketTypes
    {
        Login,
        Accept,
        Deny,
        Bind,
        Update,
        UpdateSettings,
        GetSettings,
        RemoveParticipant,
        Null
    }
}
