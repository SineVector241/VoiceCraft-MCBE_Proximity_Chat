using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VoiceCraft.Maui.Services;
using VoiceCraft.Maui.Models;

namespace VoiceCraft.Maui.ViewModels
{
    public partial class AddServerViewModel : ObservableObject
    {
        [ObservableProperty]
        ServerModel server = new ServerModel();

        [RelayCommand]
        public async Task SaveServer()
        {
            try
            {
                await Database.Instance.AddServer(Server);
                await Navigator.GoBack();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
            }
        }
    }
}
