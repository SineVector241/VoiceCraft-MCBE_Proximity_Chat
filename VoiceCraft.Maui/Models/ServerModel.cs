using CommunityToolkit.Mvvm.ComponentModel;

namespace VoiceCraft.Models
{
    public partial class ServerModel : ObservableObject
    {
        [ObservableProperty]
        string name = string.Empty;
    }
}
