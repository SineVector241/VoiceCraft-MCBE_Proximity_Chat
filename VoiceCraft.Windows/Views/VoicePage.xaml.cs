using System.Windows.Controls;
using VoiceCraft.Windows.ViewModels;

namespace VoiceCraft.Windows.Views
{
    /// <summary>
    /// Interaction logic for VoicePage.xaml
    /// </summary>
    public partial class VoicePage : Page
    {
        public VoicePage()
        {
            InitializeComponent();

            var viewModel = (VoicePageViewModel)DataContext;
            viewModel.StartConnectionCommand.Execute(null);
        }
    }
}
