using System.Collections.Generic;

namespace VoiceCraft.Mobile.Models
{
    public class DatabaseModel
    {
        public List<ServerModel> Servers { get; set; } = new List<ServerModel>();
        public SettingsModel Settings { get; set; } = new SettingsModel();
    }
}
