using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Windows;
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
            var server = Database.GetPassableObject<ServerModel>();
            this.server = new ServerModel()
            {
                IP = server.IP,
                Port = server.Port,
                Key = server.Key,
                Name = server.Name
            };
        }

        [RelayCommand]
        public void EditServer()
        {
            try
            {
                Database.UpdateServer(Server);
                Navigator.GoToPreviousPage();
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "Error!", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
        }

        [RelayCommand]
        public void Cancel()
        {
            Navigator.GoToPreviousPage();
        }
    }
}
