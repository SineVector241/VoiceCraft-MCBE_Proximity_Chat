using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using VoiceCraftProximityChat.Models;
using VoiceCraftProximityChat.Storage;
using VoiceCraftProximityChat.Views;

namespace VoiceCraftProximityChat.ViewModels
{
    public partial class ServersPageViewModel : ObservableObject
    {
        [ObservableProperty]
        ServerModel selectedServer = null;

        [ObservableProperty]
        ObservableCollection<ServerModel> servers = new ObservableCollection<ServerModel>();

        public ServersPageViewModel() 
        { 
            Servers = new ObservableCollection<ServerModel>(Database.GetServers().Servers);
        }

        [RelayCommand]
        public void GoToAddServerPage()
        {
            var navigator = (Frame)App.Current.MainWindow.FindName("Navigator");
            navigator.Navigate(new AddServerPage());
        }

        [RelayCommand]
        public void DeleteServer()
        {
            if (!ServerIsSelected())
            {
                MessageBox.Show("Please select a server first", "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            Database.DeleteServer(SelectedServer);
            Servers.Remove(SelectedServer);
        }

        [RelayCommand]
        public void Connect()
        {
            if (!ServerIsSelected())
            {
                MessageBox.Show("Please select a server first", "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            var navigator = (Frame)App.Current.MainWindow.FindName("Navigator");
            navigator.Navigate(new VoicePage(SelectedServer.Name));
        }

        private bool ServerIsSelected()
        {
            if (SelectedServer != null)
                return true;
            return false;
        }
    }
}
