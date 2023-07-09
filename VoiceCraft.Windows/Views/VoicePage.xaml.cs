using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
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
