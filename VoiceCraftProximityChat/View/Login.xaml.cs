using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using VoiceCraftProximityChat.Core.Network;

namespace VoiceCraftProximityChat.View
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class Login : Window
    {
        public Login()
        {
            InitializeComponent();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        private void btnMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
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

        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                btnConnect.IsEnabled = false;
                btnConnect.Content = "Connecting...";
                if (Network.Login(txtSIP.Text, Convert.ToInt16(txtSPort.Text), txtSKey.Text))
                {
                    new VoiceWindow().Show();
                    Close();
                }
                else
                {
                    btnConnect.IsEnabled = true;
                    btnConnect.Content = "Connect";
                    Network.Dispose();
                    MessageBox.Show("Error. Server does not exist or key was incorrect.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex}");
            }
        }

        public void OpenVoiceWindow()
        {
            new VoiceWindow().Show();
        }
    }
}
