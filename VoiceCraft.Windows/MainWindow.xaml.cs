using Microsoft.Windows.Themes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using VoiceCraft.Windows.Views;

namespace VoiceCraft.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Navigator.Window = this;
            Navigator.NavigateTo(new ServersPage());
        }

        public void Navigate(Page page)
        {
            PageContent.Navigate(page);
        }

        public void GoToPreviousPage()
        {
            PageContent.GoBack();
        }
    }
}
