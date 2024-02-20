using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using VoiceCraft.Models;

namespace VoiceCraft.Maui.ViewModels
{
    public partial class ServersViewModel : ObservableObject
    {
        [ObservableProperty]
        ObservableCollection<ServerModel> servers = new ObservableCollection<ServerModel>() { new ServerModel(), new ServerModel(), new ServerModel() };
    }
}
