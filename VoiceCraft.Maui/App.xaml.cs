namespace VoiceCraft.Maui
{
    public partial class App : Application
    {
        public const string Version = "1.0.3";
        public App()
        {
            InitializeComponent();

            MainPage = new AppShell();
        }
    }
}