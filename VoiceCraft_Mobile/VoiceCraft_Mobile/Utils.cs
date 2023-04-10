using Android.Content.Res;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace VoiceCraft_Mobile
{
    public class Utils
    {
        public delegate void PagePopped(Page page);

        public static event PagePopped OnPagePopped;

        public static async Task DisplayAlertAsync(string Title, string Description, string cancellationButton = "OK")
        {
            await App.Current.MainPage.Navigation.NavigationStack.LastOrDefault().DisplayAlert(Title, Description, cancellationButton);
        }

        public static async Task GoToPreviousPageAsync()
        {
            var page = App.Current.MainPage.Navigation.NavigationStack.LastOrDefault();
            await page.Navigation.PopAsync();
            OnPagePopped?.Invoke(page);
        }

        public static async Task<bool> CheckAndRequestPermissions()
        {
            var status = PermissionStatus.Unknown;
            status = await Permissions.CheckStatusAsync<Permissions.Microphone>();

            if (status != PermissionStatus.Granted)
            {
                if (Permissions.ShouldShowRationale<Permissions.Microphone>())
                {
                    await Shell.Current.DisplayAlert("Needs Permission", "VoiceCraft requires microphone access in order to work and communicate with other people!", "OK");
                }

                status = await Permissions.RequestAsync<Permissions.Microphone>();
            }

            if (status != PermissionStatus.Granted)
            {
                await Shell.Current.DisplayAlert("Needs Permission", "Could not login as microphone access was denied", "OK");
                return await Task.FromResult(false);
            }

            return await Task.FromResult(true);
        }
    }
}
