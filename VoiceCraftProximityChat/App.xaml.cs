using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using VoiceCraftProximityChat.Views;

namespace VoiceCraftProximityChat
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected void ApplicationStart(object sender, StartupEventArgs e)
        {
            var connectView = new Connect();
            connectView.Show();
            connectView.IsVisibleChanged += (s, ev) =>
            {
                if (connectView.IsVisible == false && connectView.IsLoaded)
                {
                    var mainView = new Main();
                    mainView.Show();
                    connectView.Close();
                }
            };
        }
    }
}
