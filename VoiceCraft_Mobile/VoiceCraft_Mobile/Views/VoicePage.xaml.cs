using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace VoiceCraft_Mobile.Views
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class VoicePage : ContentPage
	{
		public VoicePage ()
		{
			InitializeComponent ();
		}

        protected override bool OnBackButtonPressed()
        {
			return true;
        }
    }
}