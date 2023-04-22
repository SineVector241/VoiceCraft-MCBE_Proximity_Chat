using System.Windows.Controls;
using System.Windows.Input;

namespace VoiceCraftProximityChat.Views
{
    /// <summary>
    /// Interaction logic for AddServerPage.xaml
    /// </summary>
    public partial class AddServerPage : Page
    {
        public AddServerPage()
        {
            InitializeComponent();
        }

        private void NumbersOnlyTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsValid(((TextBox)sender).Text + e.Text);
        }

        public static bool IsValid(string str)
        {
            int i;
            return int.TryParse(str, out i) && i >= 1 && i <= 65535;
        }
    }
}
