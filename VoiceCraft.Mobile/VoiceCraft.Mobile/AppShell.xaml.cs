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
            Routing.RegisterRoute(nameof(AddServerPage), typeof(AddServerPage));
            Routing.RegisterRoute(nameof(VoicePage), typeof(VoicePage));
            Routing.RegisterRoute(nameof(EditPage), typeof(EditPage));
        }
    }
}