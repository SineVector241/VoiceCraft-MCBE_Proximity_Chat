using VoiceCraft.Maui.ViewModels;

namespace VoiceCraft.Maui.Views.Desktop;

public partial class EditServer : ContentPage
{
    EditServerViewModel viewModel;
	public EditServer()
	{
		InitializeComponent();
        viewModel = (EditServerViewModel)BindingContext;
	}

    private void PortEntryUnfocused(object sender, FocusEventArgs e)
    {
        if (sender is Entry)
        {
            var entry = (Entry)sender;
            var valid = int.TryParse(entry.Text, out var result);
            if (!valid)
            {
                var cleaned = new string(entry.Text.Where(char.IsDigit).ToArray());
                if (int.TryParse(cleaned, out var res))
                {
                    var clamped = Math.Clamp(res, 1025, 65535);
                    viewModel.UnsavedServer.Port = clamped;
                    entry.Text = clamped.ToString();
                }
                else
                {
                    viewModel.UnsavedServer.Port = 1025;
                }
            }
            else if (result > 65535 || result < 1025)
            {
                var clamped = Math.Clamp(result, 1025, 65535);
                viewModel.UnsavedServer.Port = clamped;
            }
        }
    }

    private void KeyEntryUnfocused(object sender, FocusEventArgs e)
    {
        if (sender is Entry)
        {
            var entry = (Entry)sender;
            var valid = int.TryParse(entry.Text, out var result);
            if (!valid)
            {
                var cleaned = new string(entry.Text.Where(char.IsDigit).ToArray());
                if (int.TryParse(cleaned, out var res))
                {
                    var clamped = Math.Clamp(res, 0, 65535);
                    viewModel.UnsavedServer.Key = (ushort)clamped;
                    entry.Text = clamped.ToString();
                }
                else
                {
                    viewModel.UnsavedServer.Key = 0;
                }
            }
            else if (result > 65535 || result < 0)
            {
                var clamped = Math.Clamp(result, 0, 65535);
                viewModel.UnsavedServer.Key = (ushort)clamped;
            }
        }
    }
}