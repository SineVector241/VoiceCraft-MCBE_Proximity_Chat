using VoiceCraft.Mobile.Views;
using Xamarin.Forms;

namespace VoiceCraft.Mobile
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(ServerPage), typeof(ServerPage));
        }
    }
}