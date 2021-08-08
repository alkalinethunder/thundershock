using Thundershock.Core.Scripting;
using Thundershock.Gui;

namespace Thundershock.ScriptLibraries
{
    [ScriptStaticLibrary("Platform")]
    public class ScriptPlatformLibrary
    {
        public void ShowMessage(string title, string message)
        {
            DialogBox.Show(title, message, DialogBoxIcon.Information, DialogBoxButtons.Ok);
        }

        public void ShowError(string title, string message)
        {
            DialogBox.ShowError(title, message);
        }
    }
}