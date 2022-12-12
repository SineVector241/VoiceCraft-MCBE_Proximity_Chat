using System.Windows;
using System.Windows.Input;
using VoiceCraftProximityChat.Network;
using VoiceCraftProximityChat.Repositories;

namespace VoiceCraftProximityChat.Views
{
    /// <summary>
    /// Interaction logic for Voice.xaml
    /// </summary>
    public partial class Main : Window
    {
        public Main()
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

        private void VolumeGainChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            AudioPlayback.volumeGain = (float)e.NewValue;
        }
    }
}
