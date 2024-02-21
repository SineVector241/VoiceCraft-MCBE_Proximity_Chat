using System.Diagnostics;

namespace VoiceCraft.Maui.Services
{
    public class Navigator
    {
        public static async Task NavigateTo(string pageName)
        {
            try
            {
                await Shell.Current.GoToAsync(pageName);
            }
            catch(Exception ex )
            {
                await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
            }
        }

        public static async Task GoBack()
        {
            try
            {
                await Shell.Current.Navigation.PopAsync();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
            }
        }
    }
}
