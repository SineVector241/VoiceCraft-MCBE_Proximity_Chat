using CommunityToolkit.Mvvm.ComponentModel;
using VoiceCraft.Core.Client;

namespace VoiceCraft.Mobile.Models
{
    public partial class ChannelDisplayModel : ObservableObject
    {
        [ObservableProperty]
        string name = string.Empty;
        [ObservableProperty]
        bool requiresPassword;
        [ObservableProperty]
        bool joined;
        [ObservableProperty]
        VoiceCraftChannel channel;

        public ChannelDisplayModel(VoiceCraftChannel channel)
        {
            this.channel = channel;
            name = channel.Name;
            requiresPassword = channel.RequiresPassword;
            joined = channel.Joined;
        }
    }
}
