using Microsoft.Toolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using VoiceCraft_Mobile.Models;

namespace VoiceCraft_Mobile.ViewModels
{
    public partial class BaseViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<Client> _clients;

        [ObservableProperty]
        private string _errorMessage;
    }
}
