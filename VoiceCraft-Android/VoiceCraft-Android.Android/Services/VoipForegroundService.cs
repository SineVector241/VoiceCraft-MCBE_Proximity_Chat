using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Java.IO;
using System.Threading;
using System.Threading.Tasks;
using VoiceCraft_Android.Services;
using Xamarin.Forms;
using static VoiceCraft_Android.Services.Messages;

namespace VoiceCraft_Android.Droid.Services
{
    [Service(ForegroundServiceType = Android.Content.PM.ForegroundService.TypeMicrophone)]
    public class VoipForegroundService : Service
    {
        CancellationTokenSource cts;
        public const int ServiceRunningNotificationId = 28234;
        public override IBinder OnBind(Intent intent)
        {
            return null;
        }

        [return: GeneratedEnum]
        public override StartCommandResult OnStartCommand(Intent intent, [GeneratedEnum] StartCommandFlags flags, int startId)
        {
            cts = new CancellationTokenSource();
            Notification notification = new NotificationHelper().GetServiceStartedNotification();
            StartForeground(ServiceRunningNotificationId, notification);

            Task.Run(() => {
                try
                {
                    var voipShared = new VoipService();
                    voipShared.Run(cts.Token, intent.GetStringExtra("ServerName")).Wait();
                }
                catch (OperationCanceledException)
                {
                }
                finally
                {
                    if (cts.IsCancellationRequested)
                    {
                        System.Console.WriteLine("Service Stopped");
                        var message = new StopServiceMessage();
                        Device.BeginInvokeOnMainThread(() => MessagingCenter.Send(message, "ServiceStopped"));
                    }
                }
            }, cts.Token);
            return StartCommandResult.Sticky;
        }

        public override void OnDestroy()
        {
            if(cts != null)
            {
                cts.Token.ThrowIfCancellationRequested();
                cts.Cancel();
            }
            base.OnDestroy();
        }
    }
}