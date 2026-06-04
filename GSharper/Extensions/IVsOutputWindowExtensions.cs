using GSharper.Helpers;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace GSharper.Extensions
{
    public static class IVsOutputWindowExtensions
    {
        public static void Output(this IVsOutputWindowPane putputWindow, CmdCommandResult result)
        {
            if (putputWindow == null || result == null)
                return;

            if (!String.IsNullOrWhiteSpace(result.Output))
                putputWindow.OutputLine(result.Output);

            if (!String.IsNullOrWhiteSpace(result.Error))
                putputWindow.OutputLine("ERROR: " + result.Error);
        }

        public static void OutputLine(this IVsOutputWindowPane putputWindow, string text)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                putputWindow.OutputStringThreadSafe($"{text?.Trim() ?? ""}{Environment.NewLine}");
            });
        }
    }
}
