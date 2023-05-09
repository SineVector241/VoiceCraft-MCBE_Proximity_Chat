using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Controls;
using VoiceCraftProximityChat.Views;

namespace VoiceCraftProximityChat.ViewModels
{
    public partial class CreditsPageViewModel : ObservableObject
    {
        [RelayCommand]
        public void Back()
        {
            var navigator = (Frame)App.Current.MainWindow.FindName("Navigator");
            navigator.Navigate(new ServersPage());
        }
    }
}
