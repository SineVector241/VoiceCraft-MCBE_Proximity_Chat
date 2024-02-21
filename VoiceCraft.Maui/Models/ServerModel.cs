using CommunityToolkit.Mvvm.ComponentModel;

namespace VoiceCraft.Models
{
    public partial class ServerModel : ObservableObject
    {
        [ObservableProperty]
        string name = string.Empty;
        [ObservableProperty]
        string iP = string.Empty;
        [ObservableProperty]
        int port = 9050;
        [ObservableProperty]
        ushort key = 0;
    }
}
