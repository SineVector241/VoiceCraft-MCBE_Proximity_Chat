using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using VoiceCraft_Mobile.Models;
using VoiceCraft_Mobile.Repositories;

namespace VoiceCraft_Mobile.ViewModels
{
    public partial class BaseViewModel : ObservableObject
    {
        [ObservableProperty]
        static ObservableCollection<ServerModel> servers = new ObservableCollection<ServerModel>(Database.GetServers());

        [ObservableProperty]
        public static ObservableCollection<ParticipantModel> participants;
    }
}
