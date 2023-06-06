using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using VoiceCraft.Mobile.Models;
using VoiceCraft.Mobile.Views;
using Xamarin.Forms;

namespace VoiceCraft.Mobile.ViewModels
{
    public partial class ServersPageViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<ServerModel> servers = new ObservableCollection<ServerModel>() { new ServerModel() { Name = "Test", IP = "127.0.0.1", Port = 9050, Key = 51 } };

        [RelayCommand]
        public async void GoToServer(ServerModel server)
        {
            Console.WriteLine(server.IP);
            await Shell.Current.GoToAsync(nameof(ServerPage));
        }
    }
}
