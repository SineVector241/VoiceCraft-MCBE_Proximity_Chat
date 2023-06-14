using System.Threading.Tasks;
using VoiceCraft.Mobile.Services;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace VoiceCraft.Mobile
{
    public class Utils
    {
        public static async Task<bool> CheckAndRequestPermissions()
        {
            var status = await Permissions.CheckStatusAsync<Permissions.Microphone>();

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
