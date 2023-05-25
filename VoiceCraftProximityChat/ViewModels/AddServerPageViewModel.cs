using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Windows;
using System.Windows.Controls;
using VoiceCraftProximityChat.Storage;
using VoiceCraftProximityChat.Views;

namespace VoiceCraftProximityChat.ViewModels
{
    public partial class AddServerPageViewModel : ObservableObject
    {
        [ObservableProperty]
        string name = "";

        [ObservableProperty]
        string ip = "";

        [ObservableProperty]
        int port = 9050;

        [RelayCommand]
        public void AddServer()
        {
            if(string.IsNullOrWhiteSpace(Name))
            {
                MessageBox.Show("Name cannot be empty!", "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            if (string.IsNullOrWhiteSpace(Ip))
            {
                MessageBox.Show("IP cannot be empty!", "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            if (Port < 1025 || Port > 65535)
            {
                MessageBox.Show("Port cannot be lower than 1025 or higher than 65535", "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            try
            {
                Database.AddServer(new Models.ServerModel() { IP = Ip, Name = Name, Port = Port });
                var navigator = (Frame)App.Current.MainWindow.FindName("Navigator");
                navigator.Navigate(new ServersPage());
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message, "Invalid Operation", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Invalid Operation", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
        }

        [RelayCommand]
        public void Cancel()
        {
            var navigator = (Frame)App.Current.MainWindow.FindName("Navigator");
            navigator.Navigate(new ServersPage());
        }
    }
}
