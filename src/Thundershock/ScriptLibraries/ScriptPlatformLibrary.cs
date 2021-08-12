using System;
using Thundershock.Core.Scripting;
using Thundershock.Gui;

namespace Thundershock.ScriptLibraries
{
    [ScriptType("Platform")]
    public static class ScriptPlatformLibrary
    {
        public static void ShowMessage(string title, string message)
        {
            DialogBox.Show(title, message, DialogBoxIcon.Information, DialogBoxButtons.Ok);
        }

        public static void ShowError(string title, string message)
        {
            DialogBox.ShowError(title, message);
        }

        public static void AskForFile(bool save, Action<string> callback)
        {
            var chooser = new FileChooser();
            chooser.AllowAnyFileType = true;

            if (save)
                chooser.FileOpenerType = FileOpenerType.Save;
            
            var result = chooser.Activate();

            if (result == FileOpenerResult.Ok)
            {
                callback?.Invoke(chooser.SelectedFilePath);
            }
        }
    }
}