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
            Server = Database.GetPassableObject<ServerModel>();
        }

        [RelayCommand]
        public async void EditServer()
        {
            try
            {
                Database.UpdateServer(Server);
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
