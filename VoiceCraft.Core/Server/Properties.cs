using System.Collections.Generic;

namespace VoiceCraft.Core.Server
{
    public class Properties
    {
        //UDP Ports
        public ushort VoicePortUDP { get; set; } = 9050;

        //TCP Ports
        public ushort SignallingPortTCP { get; set; } = 9050;
        public ushort MCCommPortTCP { get; set; } = 9051;

        //Unchangeable Settings
        public string PermanentServerKey { get; set; } = "";
        public ConnectionTypes ConnectionType { get; set; } = ConnectionTypes.Server;
        public int ExternalServerTimeoutMS { get; set; } = 5000;
        public int ClientTimeoutMS { get; set; } = 15000;
        public List<VoiceCraftChannel> Channels { get; set; } = new List<VoiceCraftChannel>();

        //Changeable Settings
        public int ProximityDistance { get; set; } = 30;
        public bool ProximityToggle { get; set; } = true;
        public bool VoiceEffects { get; set; } = true;
        public string ServerMOTD { get; set; } = "VoiceCraft Proximity Chat!";
        public DebugProperties Debugger { get; set; } = new DebugProperties();
    }

    public enum ConnectionTypes
    {
        Server,
        Client,
        Hybrid
    }
}
