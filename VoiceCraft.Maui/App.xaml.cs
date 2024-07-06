using OpusSharp.Core;

namespace VoiceCraft.Maui
{
    public partial class App : Application
    {
        public const string Version = "1.0.6";
        public static string OpusVersion = OpusInfo.Version();
        public App()
        {
            InitializeComponent();

            MainPage = new AppShell();
        }
    }
}