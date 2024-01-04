using System;

using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.OS;
using Android.Content;
using Xamarin.Forms;
using VoiceCraft.Mobile.Services;
using VoiceCraft.Mobile.Droid.Services;
using Android;
using Xamarin.Essentials;

namespace VoiceCraft.Mobile.Droid
{
    [Activity(Label = "VoiceCraft", Icon = "@drawable/vc", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize )]
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
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        protected override void OnStart()
        {
            base.OnStart();
            const int requestNotifId = 0;
            string[] notifPerm = { Manifest.Permission.PostNotifications };

            if ((int)Build.VERSION.SdkInt < 33) return;

            if (this.CheckSelfPermission(Manifest.Permission.PostNotifications) != Permission.Granted)
                this.RequestPermissions(notifPerm, requestNotifId);
        }

        private void SetServiceMethods()
        {
            Preferences.Set("VoipServiceRunning", IsServiceRunning(typeof(VoipForegroundService)));

            MessagingCenter.Subscribe<StartServiceMSG>(this, "StartService", message =>
            {
                if (!IsServiceRunning(typeof(VoipForegroundService)))
                {
                    StartForegroundService(serviceIntent);
                    Preferences.Set("VoipServiceRunning", true);
                }
            });

            MessagingCenter.Subscribe<StopServiceMSG>(this, "StopService", message =>
            {
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
                if (service.Service.ClassName.Equals(Java.Lang.Class.FromType(cls).CanonicalName))
                {
                    return true;
                }
            }
            return false;
        }
    }
}