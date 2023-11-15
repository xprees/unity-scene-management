namespace Xprees.SceneManagement
{
    public partial class SceneLoader
    {
        private void DisableAllInput()
        {
            if (disableAllInputEvent == null) return;
            disableAllInputEvent.RaiseEvent();
        }

        private void EnableGameplayInput()
        {
            if (enableGameplayInputEvent == null) return;
            enableGameplayInputEvent.RaiseEvent();
        }

        private void EnableUiInput()
        {
            if (enableUiInputEvent == null) return;
            enableUiInputEvent.RaiseEvent();
        }

        private void EnableVRInput()
        {
            // TODO something to setup VR input
        }
    }
}