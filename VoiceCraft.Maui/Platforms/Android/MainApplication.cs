using Android.App;
using Android.Runtime;
using CommunityToolkit.Mvvm.Messaging;
using VoiceCraft.Maui.Services;

namespace VoiceCraft.Maui
{
    [Application]
    public class MainApplication : MauiApplication
    {
        ServiceManager serviceManager;
        public MainApplication(IntPtr handle, JniHandleOwnership ownership)
            : base(handle, ownership)
        {
            serviceManager = new ServiceManager();

            WeakReferenceMessenger.Default.Register(this, (object recipient, StartServiceMSG message) =>
            {
                serviceManager.StartService();
            });

            WeakReferenceMessenger.Default.Register(this, (object recipient, StopServiceMSG message) =>
            {
                serviceManager.Stop();
            });
        }

        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
    }
}
