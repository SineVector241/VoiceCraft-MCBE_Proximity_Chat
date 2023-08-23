using Xamarin.Forms;

namespace VoiceCraft.Mobile.Animations
{
    public class UnhideAction : TriggerAction<VisualElement>
    {
        protected override async void Invoke(VisualElement sender)
        {
            await sender.TranslateTo(0, 0, 400);
        }
    }
}
