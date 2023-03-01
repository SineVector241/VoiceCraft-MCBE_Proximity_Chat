using System.Linq;
using System.Threading.Tasks;

namespace VoiceCraft_Mobile
{
    public class Utils
    {
        public static async Task DisplayAlertAsync(string Title, string Description, string cancellationButton = "OK")
        {
            await App.Current.MainPage.Navigation.NavigationStack.LastOrDefault().DisplayAlert(Title, Description, cancellationButton);
        }

        public static async Task GoToPreviousPageAsync()
        {
            await App.Current.MainPage.Navigation.NavigationStack.LastOrDefault().Navigation.PopAsync();
        }
    }
}
