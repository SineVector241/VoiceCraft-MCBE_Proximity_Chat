using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace VoiceCraft.Windows.ViewModels
{
    public partial class HelpPageViewModel : ObservableObject
    {
        [RelayCommand]
        public void GoBack()
        {
            Navigator.GoToPreviousPage();
        }
    }
}
