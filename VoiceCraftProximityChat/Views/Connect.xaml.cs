using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace VoiceCraftProximityChat.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class Connect : Window
    {
        public Connect()
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
    }
}
