using GSharper.Assistants;
using GSharper.Dialogs;
using GSharper.Extensions;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;

namespace GSharper.Commands
{
    public class TriggerSearchTextCommand : GSharperCommandBase<TriggerSearchTextCommand>
    {
        #region Win32 API Imports

        // Импортируем необходимые функции из системной библиотеки user32.dll
        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        // Константы для управления окнами Windows
        static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        const uint SWP_NOMOVE = 0x0002;
        const uint SWP_NOSIZE = 0x0001;

        #endregion


        public override void Execute(object sender, EventArgs e)
        {
            var dte = Assistant.GetDte();
            try
            {
                dte.ExecuteCommand("Tools.BlitzSearchThis");
            }
            catch 
            {
                Assistant.MessageBox(@"Установите плагин Blitz Search.
Перейдите по ссылке https://github.com/Natestah/BlitzSearch и установите приложение из релиза
");

                return;
            }

            Task.Run(async () =>
            {
                bool success = false;
                for (int i = 0; i < 10 && !success; i++)
                {
                    success = success || await TryUpPosition();
                }
            });
        }

        public async Task<bool> TryUpPosition()
        {
            await Task.Delay(100);
            IntPtr blitzWindowHandle = FindWindow(null, "Blitz");

            if (blitzWindowHandle != IntPtr.Zero)
            {
                SetWindowPos(
                    blitzWindowHandle,
                    HWND_TOPMOST,
                    0, 0, 0, 0,
                    SWP_NOMOVE | SWP_NOSIZE
                );

                return true;
            }

            return false;
        }
    }
}
