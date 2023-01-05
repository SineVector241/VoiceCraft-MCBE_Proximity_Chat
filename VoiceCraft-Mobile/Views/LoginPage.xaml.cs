using VoiceCraft_Mobile.ViewModels;

namespace VoiceCraft_Mobile.Views;

public partial class LoginPage : ContentPage
{
	public LoginPage(LoginPageViewModel viewModel)
	{
		InitializeComponent();
		this.BindingContext = viewModel;
	}
}