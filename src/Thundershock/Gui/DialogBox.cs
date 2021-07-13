using System;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace Thundershock.Gui
{
    public static class DialogBox
    {
        #region Win32 constants

        private const uint MbOk = 0x00;
        private const uint MbOkcancel = 0x01;
        private const uint MbYesno = 0x04;
        private const uint MbYesnocancel = 0x03;

        private const uint MbIconwarning = 0x30;
        private const uint MbIconinformation = 0x40;
        private const uint MbIconquestion = 0x20;
        private const uint MbIconstop = 0x10;

        #endregion

        // use win32 directly instead of spinning up winforms, better for sanity.
        [DllImport("user32.dll", EntryPoint = "MessageBox", CallingConvention = CallingConvention.Cdecl)]
        public static extern int Win32MessageBox(IntPtr hWnd, string message, string caption, uint type);

        public static DialogResult Show(string title, string message, DialogBoxIcon icon, DialogBoxButtons buttons)
        {
            if (ThundershockPlatform.IsPlatform(Platform.Windows))
            {
                var mainWindow = Process.GetCurrentProcess().MainWindowHandle;

                var type = icon switch
                {
                    DialogBoxIcon.Information => MbIconinformation,
                    DialogBoxIcon.Warning => MbIconwarning,
                    DialogBoxIcon.Question => MbIconquestion,
                    DialogBoxIcon.Error => MbIconstop,
                    _ => MbIconinformation
                } | buttons switch
                {
                    DialogBoxButtons.Ok => MbOk,
                    DialogBoxButtons.OkCancel => MbOkcancel,
                    DialogBoxButtons.YesNo => MbYesno,
                    DialogBoxButtons.YesNoCancel => MbYesnocancel,
                    _ => MbOk
                };

                var result = Win32MessageBox(mainWindow, message, title, type);

                return result switch
                {
                    1 => DialogResult.Ok,
                    2 => DialogResult.Cancel,
                    6 => DialogResult.Yes,
                    7 => DialogResult.No,
                    _ => DialogResult.Unknown
                };
            }

            throw new NotImplementedException();
        }

        public static void ShowError(string title, string message)
            => Show(title, message, DialogBoxIcon.Error, DialogBoxButtons.Ok);
    }
}