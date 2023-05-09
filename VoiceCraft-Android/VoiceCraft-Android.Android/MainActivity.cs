using System;

using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.OS;
using Android.Content;
using Xamarin.Forms;
using VoiceCraft_Android.Droid.Services;
using Android;
using VoiceCraft_Android.Services;
using Xamarin.Essentials;

namespace VoiceCraft_Android.Droid
{
    [Activity(Label = "VoiceCraft_Android", Icon = "@drawable/vc", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize )]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        Intent serviceIntent;
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            global::Xamarin.Forms.Forms.Init(this, savedInstanceState);

            serviceIntent = new Intent(this, typeof(VoipForegroundService));
            SetServiceMethods();

            LoadApplication(new App());
        }

        protected override void OnStart()
        {
            base.OnStart();
            const int requestNotifId = 0;
            string[] notifPerm = { Manifest.Permission.PostNotifications };

            if ((int)Build.VERSION.SdkInt < 33) return;

            if(this.CheckSelfPermission(Manifest.Permission.PostNotifications) != Permission.Granted)
                this.RequestPermissions(notifPerm, requestNotifId);
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        private void SetServiceMethods()
        {
            MessagingCenter.Subscribe<StartServiceMessage>(this, "ServiceStarted", message => {
                if (!IsServiceRunning(typeof(VoipForegroundService)))
                {
                    serviceIntent.PutExtra("ServerName", message.ServerName);
                    StartForegroundService(serviceIntent);
                    Preferences.Set("VoipServiceRunning", true);
                }
            });

            MessagingCenter.Subscribe<StopServiceMessage>(this, "ServiceStopped", message => {
                if (IsServiceRunning(typeof(VoipForegroundService)))
                {
                    StopService(serviceIntent);
                    Preferences.Set("VoipServiceRunning", false);
                }
            });
        }

        private bool IsServiceRunning(Type cls)
        {
            ActivityManager activityManager = (ActivityManager)GetSystemService(ActivityService);
            foreach (var service in activityManager.GetRunningServices(int.MaxValue))
            {
                if(service.Service.ClassName.Equals(Java.Lang.Class.FromType(cls).CanonicalName))
                {
                    return true;
                }
            }
            return false;
        }
    }
}