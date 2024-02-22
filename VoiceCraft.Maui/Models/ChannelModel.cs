using CommunityToolkit.Mvvm.ComponentModel;
using VoiceCraft.Core.Client;

namespace VoiceCraft.Maui.Models
{
    public partial class ChannelModel : ObservableObject
    {
        [ObservableProperty]
        string name = string.Empty;
        [ObservableProperty]
        bool requiresPassword;
        [ObservableProperty]
        bool joined;
        [ObservableProperty]
        VoiceCraftChannel channel;

        public ChannelModel(VoiceCraftChannel channel)
        {
            this.channel = channel;
            name = channel.Name;
            requiresPassword = channel.RequiresPassword;
            joined = channel.Joined;
        }
    }
}
