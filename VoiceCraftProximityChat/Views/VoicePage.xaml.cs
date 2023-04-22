using System.Windows.Controls;
using VoiceCraftProximityChat.ViewModels;

namespace VoiceCraftProximityChat.Views
{
    /// <summary>
    /// Interaction logic for VoicePage.xaml
    /// </summary>
    public partial class VoicePage : Page
    {
        public VoicePage(string serverName)
        {
            InitializeComponent();

            var viewModel = (VoicePageViewModel)DataContext;
            viewModel.StartConnectionCommand.Execute(serverName);
        }
    }
}
