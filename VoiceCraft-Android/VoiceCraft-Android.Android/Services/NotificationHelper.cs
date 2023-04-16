using Android.App;
using Android.Content;
using AndroidX.Core.App;

namespace VoiceCraft_Android.Droid.Services
{
    internal class NotificationHelper
    {
        private static string ForegroundChannelId = "VoiceCraft_Notifications";
        private static Context context = Application.Context;

        public Notification GetServiceStartedNotification()
        {
            var intent = new Intent(context, typeof(MainActivity));
            intent.AddFlags(ActivityFlags.SingleTop);
            intent.PutExtra("Title", "Message");

            var pendingIntent = PendingIntent.GetActivity(context, 0, intent, PendingIntentFlags.Immutable);

            var nBuilder = new NotificationCompat.Builder(context, ForegroundChannelId)
                .SetContentTitle("VoiceCraft")
                .SetContentText("Voice Ongoing...")
                .SetSmallIcon(Resource.Drawable.notification_tile_bg)
                .SetOngoing(true)
                .SetContentIntent(pendingIntent);

            NotificationChannel notificationChannel = new NotificationChannel(ForegroundChannelId, "VoiceCraft", NotificationImportance.Low);
            notificationChannel.EnableLights(true);

            var notificationManager = context.GetSystemService(Context.NotificationService) as NotificationManager;
            if(notificationManager != null)
            {
                nBuilder.SetChannelId(ForegroundChannelId);
                notificationManager.CreateNotificationChannel(notificationChannel);
            }

            return nBuilder.Build();
        }
    }
}