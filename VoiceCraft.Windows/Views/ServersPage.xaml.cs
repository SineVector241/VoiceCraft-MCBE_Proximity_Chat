using System.Windows;
using System.Windows.Controls;

namespace VoiceCraft.Windows.Views
{
    /// <summary>
    /// Interaction logic for ServersPage.xaml
    /// </summary>
    public partial class ServersPage : Page
    {
        public ServersPage()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = false;
        }
    }
}
