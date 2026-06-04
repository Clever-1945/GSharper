using GSharper.Assistants;
using GSharper.Controls;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Interop;

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
        private QuickInfoBlockControl control = null;

        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_LBUTTONUP = 0x0202;

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
                var symbol = GetSymbolUnderCursor();
                control.SetData(null, symbol, null, false);
            }
        }

        private ISymbol GetSymbolUnderCursor()
        {
            IWpfTextView textView = Assistant.GetActiveTextView();
            if (textView == null) 
                return null;

            SnapshotPoint caretPoint = textView.Caret.Position.BufferPosition;
            int position = caretPoint.Position;

            var componentModel = (IComponentModel)GetService(typeof(SComponentModel));
            var workspace = componentModel.GetService<VisualStudioWorkspace>();
            if (workspace == null) 
                return null;

            Document document = caretPoint.Snapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null) 
                return null;

            if (!document.TryGetSemanticModel(out var semanticModel))
                return null;

            if (!document.TryGetSyntaxRoot(out var root))
                return null;

            SyntaxNode node = root.FindToken(position).Parent;
            if (node == null)
                return null;

            SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(node);
            ISymbol symbol = symbolInfo.Symbol ?? symbolInfo.CandidateSymbols.FirstOrDefault() ?? semanticModel.GetDeclaredSymbol(node);
            return symbol;
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
