using OpusSharp.Core;

namespace VoiceCraft.Maui
{
    public partial class App : Application
    {
        public static string Version = AppInfo.Current.VersionString;
        public static string OpusVersion = OpusInfo.Version();
        public App()
        {
            InitializeComponent();

            MainPage = new AppShell();
        }
    }
}