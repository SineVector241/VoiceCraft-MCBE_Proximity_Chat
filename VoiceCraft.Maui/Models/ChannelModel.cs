using CommunityToolkit.Mvvm.ComponentModel;
using VoiceCraft.Core;

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
        Channel channel;

        public ChannelModel(Channel channel)
        {
            this.channel = channel;
            name = channel.Name;
            requiresPassword = !string.IsNullOrWhiteSpace(channel.Password);
        }
    }
}
