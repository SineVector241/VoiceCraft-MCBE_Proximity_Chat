using System.Linq;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace VoiceCraft_Android
{
    public class Utils
    {
        public static Task DisplayAlert(string Title, string Description, string cancellationButton = "OK")
        {
            Device.BeginInvokeOnMainThread(async () =>
            {
                await App.Current.MainPage.Navigation.NavigationStack.LastOrDefault().DisplayAlert(Title, Description, cancellationButton);
            });

            return Task.CompletedTask;
        }

        public static Task GoToPreviousPage()
        {
            Device.BeginInvokeOnMainThread(async () =>
            {
                var page = App.Current.MainPage.Navigation.NavigationStack.LastOrDefault();
                await page.Navigation.PopAsync();
            });

            return Task.CompletedTask;
        }

        public static Task PushPage(Page page)
        {
            Device.BeginInvokeOnMainThread(async () =>
            {
                await App.Current.MainPage.Navigation.PushAsync(page);
            });

            return Task.CompletedTask;
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
                await Shell.Current.DisplayAlert("Needs Permission", "Could not connect as microphone access was denied", "OK");
                return await Task.FromResult(false);
            }

            return await Task.FromResult(true);
        }
    }
}
