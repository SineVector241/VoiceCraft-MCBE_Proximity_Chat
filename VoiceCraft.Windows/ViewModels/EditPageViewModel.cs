using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VoiceCraft.Windows.Models;
using VoiceCraft.Windows.Storage;

namespace VoiceCraft.Windows.ViewModels
{
    public partial class EditPageViewModel : ObservableObject
    {
        [ObservableProperty]
        ServerModel server;

        public EditPageViewModel()
        {
            Server = Database.GetPassableObject<ServerModel>();
        }

        [RelayCommand]
        public void EditServer()
        {
            Database.UpdateServer(Server);
            Navigator.GoToPreviousPage();
        }

        [RelayCommand]
        public void Cancel()
        {
            Navigator.GoToPreviousPage();
        }
    }
}
