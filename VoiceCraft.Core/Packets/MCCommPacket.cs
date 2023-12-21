using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using VoiceCraft.Core.Packets.Interfaces;
using VoiceCraft.Core.Packets.MCComm;

namespace VoiceCraft.Core.Packets
{
    public class MCCommPacket : IMCCommPacket
    {
        public MCCommPacketTypes PacketType { get; set; } = MCCommPacketTypes.Null;
        public IMCCommPacketData PacketData { get; set; }

        public MCCommPacket()
        {
            PacketData = new Null();
        }

        public MCCommPacket(string data)
        {
            PacketData = new Null();

            var jsonData = JObject.Parse(data);
            if (jsonData != null)
            {
                var type = jsonData["PacketType"]?.Value<int>();
                if (type == null)
                    throw new JsonReaderException("Invalid content!");

                PacketType = (MCCommPacketTypes)type;
                switch (PacketType)
                {
                    case MCCommPacketTypes.Login:
                        var loginData = jsonData["PacketData"]?.ToObject<Login>();
                        if (loginData != null)
                            PacketData = loginData;
                        else
                            throw new JsonReaderException("Invalid data!");
                        break;
                    case MCCommPacketTypes.Accept:
                        var acceptData = jsonData["PacketData"]?.ToObject<Accept>();
                        if (acceptData != null)
                            PacketData = acceptData;
                        else
                            throw new JsonReaderException("Invalid data!");
                        break;
                    case MCCommPacketTypes.Deny:
                        var denyData = jsonData["PacketData"]?.ToObject<Deny>();
                        if (denyData != null)
                            PacketData = denyData;
                        else
                            throw new JsonReaderException("Invalid data!");
                        break;
                    case MCCommPacketTypes.Bind:
                        var bindData = jsonData["PacketData"]?.ToObject<Bind>();
                        if (bindData != null)
                            PacketData = bindData;
                        else
                            throw new JsonReaderException("Invalid data!");
                        break;
                    case MCCommPacketTypes.Update:
                        var updateData = jsonData["PacketData"]?.ToObject<Update>();
                        if (updateData != null)
                            PacketData = updateData;
                        else
                            throw new JsonReaderException("Invalid data!");
                        break;
                    case MCCommPacketTypes.UpdateSettings:
                        var updateSettingsData = jsonData["PacketData"]?.ToObject<UpdateSettings>();
                        if (updateSettingsData != null)
                            PacketData = updateSettingsData;
                        else
                            throw new JsonReaderException("Invalid data!");
                        break;
                    case MCCommPacketTypes.GetSettings:
                        var getSettingsData = jsonData["PacketData"]?.ToObject<GetSettings>();
                        if (getSettingsData != null)
                            PacketData = getSettingsData;
                        else
                            throw new JsonReaderException("Invalid data!");
                        break;
                    case MCCommPacketTypes.RemoveParticipant:
                        var removeParticipantData = jsonData["PacketData"]?.ToObject<RemoveParticipant>();
                        if (removeParticipantData != null)
                            PacketData = removeParticipantData;
                        else
                            throw new JsonReaderException("Invalid data!");
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
