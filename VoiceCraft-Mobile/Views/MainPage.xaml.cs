using VoiceCraft_Mobile.ViewModels;

namespace VoiceCraft_Mobile.Views;

public partial class MainPage : ContentPage
{
	public MainPage(MainPageViewModel viewModel)
	{
		InitializeComponent();
		this.BindingContext = viewModel;
	}
}