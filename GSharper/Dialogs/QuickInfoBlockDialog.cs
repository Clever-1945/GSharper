using GSharper.Assistants;
using GSharper.Controls;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Elfie.Model;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace GSharper.Dialogs
{
    /// <summary>
    /// This class implements the tool window exposed by this package and hosts a user control.
    /// </summary>
    /// <remarks>
    /// In Visual Studio tool windows are composed of a frame (implemented by the shell) and a pane,
    /// usually implemented by the package implementer.
    /// <para>
    /// This class derives from the ToolWindowPane class provided from the MPF in order to use its
    /// implementation of the IVsUIElementPane interface.
    /// </para>
    /// </remarks>
    [Guid("bf438e32-f64a-4043-ada2-1e5197c8316a")]
    public class QuickInfoBlockDialog : ToolWindowPane
    {
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);


        private QuickInfoBlockControl control = null;

        private const int WM_LBUTTONUP = 0x0202;
        private ISymbol _symbol = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="QuickInfoBlockDialog"/> class.
        /// </summary>
        public QuickInfoBlockDialog() : base(null)
        {
            this.Caption = "Символ под курсором";
            control = new QuickInfoBlockControl();
            this.Content = control;
        }

        public override void OnToolWindowCreated()
        {
            base.OnToolWindowCreated();
            ComponentDispatcher.ThreadPreprocessMessage += OnThreadPreprocessMessage;
        }

        private void OnThreadPreprocessMessage(ref MSG msg, ref bool handled)
        {
            if (msg.message == WM_LBUTTONUP)
            {
                var wpfTextView = GetWpfTextView(msg);
                if (wpfTextView != null)
                {
                    var symbol = Assistant.GetSymbolUnderCursor(wpfTextView);
                    if (symbol != null)
                    {
                        if (_symbol != symbol && !SymbolEqualityComparer.Default.Equals(_symbol, symbol))
                        {
                            _symbol = symbol;
                            control.SetData(null, symbol, null, false);
                        }
                    }
                }
            }
        }

        private IWpfTextView GetWpfTextView(MSG msg)
        {
            HwndSource hwndSource = HwndSource.FromHwnd(msg.hwnd);
            if (hwndSource != null && hwndSource.RootVisual is Visual rootVisual)
            {
                int x = (short)(msg.lParam.ToInt32() & 0xFFFF);
                int y = (short)((msg.lParam.ToInt32() >> 16) & 0xFFFF);
                Point clickPoint = new Point(x, y);

                DependencyObject hitElement = null;
                VisualTreeHelper.HitTest(
                    rootVisual,
                    null,
                    result => { hitElement = result.VisualHit; return HitTestResultBehavior.Stop; },
                    new PointHitTestParameters(clickPoint)
                    );

                IWpfTextView wpfTextView = FindWpfTextViewInAncestors(hitElement);

                if (wpfTextView != null)
                {
                    if (wpfTextView.Roles.Contains(PredefinedTextViewRoles.Document))
                    {
                        return wpfTextView;
                    }
                }
            }

            return null;
        }

        private IWpfTextView FindWpfTextViewInAncestors(DependencyObject element)
        {
            while (element != null)
            {
                if (element is IWpfTextViewHost host) return host.TextView;
                if (element is IWpfTextView textView) return textView;

                element = VisualTreeHelper.GetParent(element);
            }
            return null;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ComponentDispatcher.ThreadPreprocessMessage -= OnThreadPreprocessMessage;
            }
            base.Dispose(disposing);
        }
    }
}
