using CommunityToolkit.Mvvm.Messaging;
using VoiceCraft.Maui.Services;

namespace VoiceCraft.Maui
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            //Routing
            if(DeviceInfo.Idiom == DeviceIdiom.Phone)
            {
                Routing.RegisterRoute(nameof(Views.Mobile.Servers), typeof(Views.Mobile.Servers));
                Routing.RegisterRoute(nameof(Views.Mobile.ServerDetails), typeof(Views.Mobile.ServerDetails));
                Routing.RegisterRoute(nameof(Views.Mobile.AddServer), typeof(Views.Mobile.AddServer));
                Routing.RegisterRoute(nameof(Views.Mobile.Voice), typeof(Views.Mobile.Voice));
                Routing.RegisterRoute(nameof(Views.Mobile.Settings), typeof(Views.Mobile.Settings));
                Routing.RegisterRoute(nameof(Views.Mobile.Credits), typeof(Views.Mobile.Credits));
                Routing.RegisterRoute(nameof(Views.Mobile.EditServer), typeof(Views.Mobile.EditServer));
            }
            else
            {
                CurrentItem = desktopDefault;

                Routing.RegisterRoute(nameof(Views.Desktop.Servers), typeof(Views.Desktop.Servers));
                Routing.RegisterRoute(nameof(Views.Desktop.ServerDetails), typeof(Views.Desktop.ServerDetails));
                Routing.RegisterRoute(nameof(Views.Desktop.AddServer), typeof(Views.Desktop.AddServer));
                Routing.RegisterRoute(nameof(Views.Desktop.Voice), typeof(Views.Desktop.Voice));
                Routing.RegisterRoute(nameof(Views.Desktop.Settings), typeof(Views.Desktop.Settings));
                Routing.RegisterRoute(nameof(Views.Desktop.Credits), typeof(Views.Desktop.Credits));
                Routing.RegisterRoute(nameof(Views.Desktop.EditServer), typeof(Views.Desktop.EditServer));
            }
        }

        protected override void OnAppearing()
        {
#if !WINDOWS
            if (Preferences.Get("VoipServiceRunning", false))
            {
                MainThread.BeginInvokeOnMainThread(async () => await Navigator.NavigateTo(nameof(Views.Desktop.Voice)));
            }
#endif
            base.OnAppearing();
        }

        protected override void OnDisappearing()
        {
#if WINDOWS
            Preferences.Set("VoipServiceRunning", false);
#endif
            base.OnDisappearing();
        }
    }
}
