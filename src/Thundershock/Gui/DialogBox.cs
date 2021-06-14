using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Net.Http;

namespace Thundershock.Gui
{
    public static class DialogBox
    {
        #region Win32 constants

        private const uint MB_OK = 0x00;
        private const uint MB_OKCANCEL = 0x01;
        private const uint MB_YESNO = 0x04;
        private const uint MB_YESNOCANCEL = 0x03;

        private const uint MB_ICONWARNING = 0x30;
        private const uint MB_ICONINFORMATION = 0x40;
        private const uint MB_ICONQUESTION = 0x20;
        private const uint MB_ICONSTOP = 0x10;

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
                    DialogBoxIcon.Information => MB_ICONINFORMATION,
                    DialogBoxIcon.Warning => MB_ICONWARNING,
                    DialogBoxIcon.Question => MB_ICONQUESTION,
                    DialogBoxIcon.Error => MB_ICONSTOP,
                    _ => MB_ICONINFORMATION
                } | buttons switch
                {
                    DialogBoxButtons.Ok => MB_OK,
                    DialogBoxButtons.OkCancel => MB_OKCANCEL,
                    DialogBoxButtons.YesNo => MB_YESNO,
                    DialogBoxButtons.YesNoCancel => MB_YESNOCANCEL,
                    _ => MB_OK
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