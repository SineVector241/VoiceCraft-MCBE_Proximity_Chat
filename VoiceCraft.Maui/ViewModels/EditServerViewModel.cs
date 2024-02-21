using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VoiceCraft.Maui.Services;
using VoiceCraft.Models;

namespace VoiceCraft.Maui.ViewModels
{
    public partial class EditServerViewModel : ObservableObject
    {
        [ObservableProperty]
        ServerModel unsavedServer;

        public EditServerViewModel()
        {
            unsavedServer = (ServerModel)Navigator.GetNavigationData<ServerModel>().Clone();
        }

        [RelayCommand]
        public async Task SaveServer()
        {
            try
            {
                await Database.Instance.EditServer(UnsavedServer);
                await Navigator.GoBack();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
            }
        }
    }
}
