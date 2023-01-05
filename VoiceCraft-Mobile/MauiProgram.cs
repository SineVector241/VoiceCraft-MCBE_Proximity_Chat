using VoiceCraft_Mobile.ViewModels;
using VoiceCraft_Mobile.Views;

namespace VoiceCraft_Mobile
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            //Views
            builder.Services.AddSingleton<LoginPage>();
            builder.Services.AddSingleton<MainPage>();

            //ViewModels
            builder.Services.AddSingleton<LoginPageViewModel>();
            builder.Services.AddSingleton<MainPageViewModel>();

            return builder.Build();
        }
    }
}