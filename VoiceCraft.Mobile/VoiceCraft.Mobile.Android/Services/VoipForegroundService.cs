using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using System.Threading;
using System.Threading.Tasks;
using VoiceCraft.Mobile.Services;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace VoiceCraft.Mobile.Droid.Services
{
    [Service(ForegroundServiceType = Android.Content.PM.ForegroundService.TypeMicrophone)]
    public class VoipForegroundService : Service
    {
        CancellationTokenSource cts;
        public const int ServiceRunningNotificationId = 28234;
        private VoipService voipService;
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

            Task.Run(() =>
            {
                try
                {
                    voipService = new VoipService();
                    voipService.OnUpdate += OnUpdate;
                    voipService.OnServiceDisconnect += ServiceDisconnect;

                    MessagingCenter.Subscribe<DisconnectMessage>(this, "Disconnect", message =>
                    {
                        cts.Cancel();
                    });

                    MessagingCenter.Subscribe<MuteUnmuteMessage>(this, "MuteUnmute", message =>
                    {
                        voipService.MuteUnmute();
                    });

                    voipService.Start(cts.Token).Wait();
                }
                catch (System.OperationCanceledException)
                {
                }
                finally
                {
                    MessagingCenter.Unsubscribe<DisconnectMessage>(this, "Disconnect");
                    MessagingCenter.Unsubscribe<MuteUnmuteMessage>(this, "MuteUnmute");

                    var message = new StopServiceMessage();
                    Device.BeginInvokeOnMainThread(() => {
                        MessagingCenter.Send(message, "ServiceStopped");
                        Preferences.Set("VoipServiceRunning", false);
                    });

                    voipService.OnUpdate -= OnUpdate;
                    voipService.OnServiceDisconnect -= ServiceDisconnect;
                }
            }, cts.Token);
            return StartCommandResult.Sticky;
        }

        public override void OnDestroy()
        {
            try
            {
                if (cts != null)
                {
                    cts.Token.ThrowIfCancellationRequested();
                    cts.Cancel();
                    Preferences.Set("VoipServiceRunning", false);
                }
            }
            catch(System.OperationCanceledException)
            {
                //Do nothing
            }
            base.OnDestroy();
        }

        private void ServiceDisconnect(string Reason)
        {
            Device.BeginInvokeOnMainThread(() => {
                var message = new DisconnectMessage()
                {
                    Reason = Reason
                };

                MessagingCenter.Send(message, "Disconnect");

                cts.Cancel();
            });
        }

        private void OnUpdate(UpdateUIMessage Data)
        {
            Device.BeginInvokeOnMainThread(() => {
                MessagingCenter.Send(Data, "Update");
            });
        }
    }
}