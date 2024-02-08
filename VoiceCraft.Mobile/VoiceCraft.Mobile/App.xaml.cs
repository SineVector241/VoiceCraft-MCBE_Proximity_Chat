using Xamarin.Forms;

namespace VoiceCraft.Mobile
{
    public partial class App : Application
    {
        public const string Version = "v1.0.12";

        public App()
        {
            InitializeComponent();

            MainPage = new AppShell();
        }

        protected override void OnStart()
        {
        }

        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
        }
    }
}
