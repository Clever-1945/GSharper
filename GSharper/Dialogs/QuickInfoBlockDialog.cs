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
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Input;

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
    public class QuickInfoBlockDialog : ToolWindowPane, IVsSelectionEvents
    {
        private QuickInfoBlockControl control = null;
        private IVsMonitorSelection _monitorSelection  = null;
        private uint _cookie;
        private IWpfTextView _textView = null;

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
            _monitorSelection = ServiceProvider.GlobalProvider.GetService(typeof(SVsShellMonitorSelection)) as IVsMonitorSelection;

            if (_monitorSelection != null)
            {
                _monitorSelection.AdviseSelectionEvents(this, out _cookie);
            }
        }

        private void OnPositionChanged(object sender, Microsoft.VisualStudio.Text.Editor.CaretPositionChangedEventArgs e)
        {
            bool movedByMouse = Mouse.LeftButton == MouseButtonState.Pressed || Mouse.RightButton == MouseButtonState.Pressed;

            if (movedByMouse)
            {
                var symbol = GetSymbolUnderCursor();
                control.SetData(null, symbol, null, false);
            }
        }

        private ISymbol GetSymbolUnderCursor()
        {
            IWpfTextView textView = GetActiveTextView();
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

        private IWpfTextView GetActiveTextView()
        {
            var textManager = (IVsTextManager)GetService(typeof(SVsTextManager));
            if (textManager == null)
                return null;

            textManager.GetActiveView(1, null, out IVsTextView textViewCurrent);
            if (textViewCurrent == null) 
                return null;

            var componentModel = (IComponentModel)GetService(typeof(SComponentModel));
            var editorAdapterFactory = componentModel.GetService<IVsEditorAdaptersFactoryService>();

            return editorAdapterFactory.GetWpfTextView(textViewCurrent);
        }

        public int OnElementValueChanged(uint elementid, object varOldValue, object varNewValue)
        {
            if (elementid == (uint)VSConstants.VSSELELEMID.SEID_WindowFrame)
            {
                if (varNewValue is IVsWindowFrame frame)
                {
                    frame.GetProperty((int)__VSFPROPID.VSFPROPID_pszMkDocument, out object pathObj);
                    if (pathObj is string filePath)
                    {
                        var textView = GetActiveTextView();
                        if (textView != null)
                        {
                            textView.Caret.PositionChanged += OnPositionChanged;
                            textView.Selection.SelectionChanged += OnSelectionChanged;

                            if (_textView != null)
                            {
                                _textView.Caret.PositionChanged -= OnPositionChanged;
                                _textView.Selection.SelectionChanged -= OnSelectionChanged;
                            }

                            _textView = textView;
                        }
                    }
                }
            }
            return VSConstants.S_OK;
        }

        private void OnSelectionChanged(object sender, EventArgs e)
        {
            var selection = (ITextSelection)sender;
            if (!selection.IsEmpty)
            {
                string selectedText = selection.StreamSelectionSpan.GetText();
                control.SetExpressionSelected(selectedText);
            }
            else 
            {
                control.SetExpressionSelected(null);
            }
        }

        // Метод контекста команд (можно оставить пустым)
        public int OnCmdUIContextChanged(uint dwCmdUIContextCookie, int fActive) => VSConstants.S_OK;

        public int OnSelectionChanged(IVsHierarchy pHierOld, uint itemidOld, IVsMultiItemSelect pMISOld, ISelectionContainer pSCOld, IVsHierarchy pHierNew, uint itemidNew, IVsMultiItemSelect pMISNew, ISelectionContainer pSCNew)
        {
            if (pHierNew != null)
            {
                pHierNew.GetProperty(itemidNew, (int)__VSHPROPID.VSHPROPID_Name, out object name);
            }
            return VSConstants.S_OK;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_monitorSelection != null && _cookie != 0)
                {
                    _monitorSelection.UnadviseSelectionEvents(_cookie);
                }

                if (_textView != null)
                {
                    _textView.Caret.PositionChanged -= OnPositionChanged;
                }
                _textView = null;
            }
            base.Dispose(disposing);
        }
    }
}
