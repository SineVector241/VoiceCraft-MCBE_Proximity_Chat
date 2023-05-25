using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using VoiceCraft_Android.Models;
using VoiceCraft_Android.Storage;

namespace VoiceCraft_Android.ViewModels
{
    public partial class AddServerPageViewModel : ObservableObject
    {
        [ObservableProperty]
        string name = string.Empty;

        [ObservableProperty]
        string ip = string.Empty;

        [ObservableProperty]
        int port = 9050;

        [RelayCommand]
        public void Save()
        {
            if (string.IsNullOrEmpty(Name))
            {
                Utils.DisplayAlert("Error", "Name cannot be empty!");
                return;
            }
            if(string.IsNullOrEmpty(Ip))
            {
                Utils.DisplayAlert("Error", "IP cannot be empty!");
                return;
            }
            if(Port < 1025 || Port > 65535)
            {
                Utils.DisplayAlert("Error", "Port cannot be lower than 1025 or higher than 65535");
                return;
            }

            var server = new ServerModel() { Name = Name, IP = Ip, Port = Port };
            
            try
            {
                Database.AddServer(server);
                Utils.GoToPreviousPage();
            }
            catch (InvalidOperationException ex)
            {
                Utils.DisplayAlert("Invalid Operation", ex.Message);
            }
            catch(Exception ex)
            {
                Utils.DisplayAlert("Error", ex.Message);
            }
        }
    }
}
