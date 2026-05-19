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

        /// <summary>
        /// Initializes a new instance of the <see cref="QuickInfoBlockDialog"/> class.
        /// </summary>
        public QuickInfoBlockDialog() : base(null)
        {
            this.Caption = "QuickInfoBlockDialog";
            control = new QuickInfoBlockControl();
            this.Content = control;
        }

        public override void OnToolWindowCreated()
        {
            base.OnToolWindowCreated();
            ThreadHelper.ThrowIfNotOnUIThread();

            _monitorSelection = ServiceProvider.GlobalProvider.GetService(typeof(SVsShellMonitorSelection)) as IVsMonitorSelection;


            if (_monitorSelection != null)
            {
                _monitorSelection.AdviseSelectionEvents(this, out _cookie);
            }
        }

        private void OnPositionChanged(object sender, Microsoft.VisualStudio.Text.Editor.CaretPositionChangedEventArgs e)
        {
            var symbol = GetSymbolUnderCursor();
            // public QuickInfoBlockControl SetData(IAsyncQuickInfoSession session, ISymbol symbol, SyntaxNode node, bool hideOther = true)
            control.SetData(null, symbol, null, false);
        }

        private ISymbol GetSymbolUnderCursor()
        {
            // 1. Получаем текстовое представление редактора
            IWpfTextView textView = GetActiveTextView();
            if (textView == null) 
                return null;

            // Находим позицию каретки
            SnapshotPoint caretPoint = textView.Caret.Position.BufferPosition;
            int position = caretPoint.Position;

            // 2. Получаем Workspace Visual Studio для доступа к Roslyn
            var componentModel = (IComponentModel)GetService(typeof(SComponentModel));
            var workspace = componentModel.GetService<VisualStudioWorkspace>();
            if (workspace == null) 
                return null;

            // Находим текущий Roslyn-документ по буферу текста
            Document document = caretPoint.Snapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null) 
                return null;

            // 3. Получаем семантическую модель документа
            if (!document.TryGetSemanticModel(out var semanticModel))
                return null;

            // 4. Находим узел синтаксического дерева в позиции курсора
            if (!document.TryGetSyntaxRoot(out var root))
                return null;

            SyntaxNode node = root.FindToken(position).Parent;
            if (node == null)
                return null;

            // 5. Запрашиваем символ (работает для вызовов методов, типов, переменных)
            SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(node);
            ISymbol symbol = symbolInfo.Symbol ?? symbolInfo.CandidateSymbols.FirstOrDefault();

            // Если символ не найден напрямую, возможно курсор стоит на самом объявлении (например, class MyClass)
            if (symbol == null)
            {
                symbol = semanticModel.GetDeclaredSymbol(node);
            }

            return symbol;
        }

        private IWpfTextView GetActiveTextView()
        {
            // Получаем менеджер текстовых окон
            var textManager = (IVsTextManager)GetService(typeof(SVsTextManager));
            if (textManager == null)
                return null;

            // Находим активное окно документа
            textManager.GetActiveView(1, null, out IVsTextView textViewCurrent);
            if (textViewCurrent == null) 
                return null;

            // Адаптируем под интерфейс WPF-редактора через MEF ComponentModel
            var componentModel = (IComponentModel)GetService(typeof(SComponentModel));
            var editorAdapterFactory = componentModel.GetService<IVsEditorAdaptersFactoryService>();

            return editorAdapterFactory.GetWpfTextView(textViewCurrent);
        }

        // 1. Отслеживание смены активного окна или документа
        public int OnElementValueChanged(uint elementid, object varOldValue, object varNewValue)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

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
                        }
                    }
                }
            }
            return VSConstants.S_OK;
        }

        // Метод контекста команд (можно оставить пустым)
        public int OnCmdUIContextChanged(uint dwCmdUIContextCookie, int fActive) => VSConstants.S_OK;

        public int OnSelectionChanged(IVsHierarchy pHierOld, uint itemidOld, IVsMultiItemSelect pMISOld, ISelectionContainer pSCOld, IVsHierarchy pHierNew, uint itemidNew, IVsMultiItemSelect pMISNew, ISelectionContainer pSCNew)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // Если pHierNew не null, значит фокус на элементе дерева проекта
            if (pHierNew != null)
            {
                // Можно получить имя выбранного файла/проекта в Solution Explorer
                pHierNew.GetProperty(itemidNew, (int)__VSHPROPID.VSHPROPID_Name, out object name);
                System.Diagnostics.Debug.WriteLine($"Выделен элемент в дереве: {name}");
            }
            return VSConstants.S_OK;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                if (_monitorSelection != null && _cookie != 0)
                {
                    _monitorSelection.UnadviseSelectionEvents(_cookie);
                }
            }
            base.Dispose(disposing);
        }
    }
}
