using System;
using System.Collections.Generic;
using System.Text;

namespace VoiceCraft.Mobile.Models
{
    public class DatabaseModel
    {
#nullable enable
        public List<ServerModel>? Servers { get; set; } = new List<ServerModel>();
        public SettingsModel? Settings { get; set; } = new SettingsModel();
    }
}
