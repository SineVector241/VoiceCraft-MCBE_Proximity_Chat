using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Windows.Forms;
using VoiceCraft.Windows.Models;
using VoiceCraft.Windows.Storage;

namespace VoiceCraft.Windows.ViewModels
{
    public partial class AddServerPageViewModel : ObservableObject
    {
        [ObservableProperty]
        public ServerModel server = new ServerModel();

        [RelayCommand]
        public void AddServer()
        {
            try
            {
                Database.AddServer(Server);
                Navigator.GoToPreviousPage();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        [RelayCommand]
        public static void Cancel()
        {
            Navigator.GoToPreviousPage();
        }
    }
}
