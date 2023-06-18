using System.Windows.Controls;

namespace VoiceCraft.Windows
{
    public static class Navigator
    {
        public static MainWindow? Window;

        public static void NavigateTo(Page newPage)
        {
            Window?.Navigate(newPage);
        }

        public static void GoToPreviousPage()
        {
            Window?.GoToPreviousPage();
        }
    }
}