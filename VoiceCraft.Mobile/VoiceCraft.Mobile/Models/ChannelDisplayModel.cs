using CommunityToolkit.Mvvm.ComponentModel;
using VoiceCraft.Core.Client;

namespace VoiceCraft.Mobile.Models
{
    public partial class ChannelDisplayModel : ObservableObject
    {
        [ObservableProperty]
        public string name = string.Empty;
        [ObservableProperty]
        public bool requiresPassword;
        [ObservableProperty]
        public bool joined;
        [ObservableProperty]
        public VoiceCraftChannel channel;

        public ChannelDisplayModel(VoiceCraftChannel channel)
        {
            this.channel = channel;
            name = channel.Name;
            requiresPassword = channel.RequiresPassword;
            joined = channel.Joined;
        }
    }
}
