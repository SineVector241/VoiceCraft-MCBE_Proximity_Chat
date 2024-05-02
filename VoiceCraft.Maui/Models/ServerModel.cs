using CommunityToolkit.Mvvm.ComponentModel;

namespace VoiceCraft.Maui.Models
{
    public partial class ServerModel : ObservableObject, ICloneable
    {
        [ObservableProperty]
        string name = string.Empty;
        [ObservableProperty]
        string iP = string.Empty;
        [ObservableProperty]
        int port = 9050;
        [ObservableProperty]
        short key = 0;

        public object Clone()
        {
            return MemberwiseClone();
        }
    }
}
