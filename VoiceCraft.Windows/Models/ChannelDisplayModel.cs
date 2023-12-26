using CommunityToolkit.Mvvm.ComponentModel;
using VoiceCraft.Core.Client;

namespace VoiceCraft.Windows.Models
{
    public partial class ChannelDisplayModel : ObservableObject
    {
        [ObservableProperty]
        public VoiceCraftChannel? channel;
    }
}
