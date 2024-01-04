using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using VoiceCraft.Mobile.Models;
using VoiceCraft.Mobile.Storage;
using Xamarin.Forms;

namespace VoiceCraft.Mobile.ViewModels
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
        public async void EditServer()
        {
            try
            {
                Database.UpdateServer(Server);
                Database.SetPassableObject(Server);
                await Shell.Current.Navigation.PopAsync();
            }
            catch(Exception ex)
            {
                await Shell.Current.DisplayAlert("Error!", ex.Message, "OK");
            }
        }

        [RelayCommand]
        public void Cancel()
        {
            Shell.Current.Navigation.PopAsync();
        }
    }
}
