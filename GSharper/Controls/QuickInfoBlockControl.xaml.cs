using EnvDTE;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Elfie.Model;
using Microsoft.VisualStudio.Language.Intellisense;
using GSharper.Extensions;
using GSharper.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Xml.Linq;

namespace GSharper.Controls
{
    /// <summary>
    /// Логика взаимодействия для QuickInfoBlockControl.xaml
    /// </summary>
    public partial class QuickInfoBlockControl : UserControl
    {
        internal enum CommandMethodSymbol
        {
            /// <summary>
            /// Найти все реализации
            /// </summary>
            GoToOrFindImplementations,

            /// <summary>
            /// Найти базовые определения
            /// </summary>
            GoToOrFindBase,
        }

        private ISymbol _symbol;
        private SyntaxNode _node;
        private string _commentStringXml;
        private XElement _commentXml;
        private string _expressionToEvaluate;
        private IAsyncQuickInfoSession _session;

        public QuickInfoBlockControl(IAsyncQuickInfoSession session, ISymbol symbol, SyntaxNode node)
        {
            InitializeComponent();
            _symbol = symbol;
            _node = node;
            _session = session;
            _commentStringXml = _symbol.GetDocumentationCommentXml();
            _commentXml = CommentXml();
            _expressionToEvaluate = GetExpressionToEvaluate();
            this.AddHandler(Hyperlink.ClickEvent, new RoutedEventHandler(OnHyperlinkClicked), true);

            Render();
            this.Loaded += OnLoaded;
        }

        private void OnHyperlinkClicked(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is Hyperlink link)
            {
                if (link.Tag is ISymbol symbol)
                {
                    symbol.TryGoToDefinitionAsync();
                }
            }
        }

        private ISymbol GetRuntimeType()
        {
            if (_expressionToEvaluate == null)
            {
                return null;
            }
            string expressionToEvaluate = $"{_expressionToEvaluate}.GetType().Namespace + \".\" + {_expressionToEvaluate}.GetType().Name";
            try
            {
                var eval = Assistant.GetDte().Debugger.GetExpression(expressionToEvaluate);
                if (!eval.IsValidValue)
                {
                    return null;
                }

                var typeName= eval.Value;
                var index = typeName.LastIndexOf('`');
                if (index >= 0)
                {
                    typeName = typeName.Substring(0, index);
                }
                typeName = typeName.TrimEnd('\"').TrimStart('\"');
                return Assistant.GetWorkspace().SearchTypeByName(typeName);
            }
            catch
            {
                return null;
            }
        }

        private string GetExpressionToEvaluate()
        {
            if(_symbol is ILocalSymbol _localSymbol)
            {
                return _localSymbol.Name;
            }
            if (_node?.Parent?.GetType().Name == "MemberAccessExpressionSyntax")
            {
                return _node.Parent.ToString();
            }
            return null;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            ClearStandart();
        }

        private XElement CommentXml()
        {
            try
            {
                return XElement.Parse(_commentStringXml);
            }
            catch { }

            return null;
        }

        private void ClearStandart()
        {
            var parent = VisualTreeHelper.GetParent(this);

            while (parent != null && !(parent is ItemsControl))
            {
                parent = VisualTreeHelper.GetParent(parent);
            }

            if (parent is ItemsControl itemsControl)
            {
                foreach (var item in itemsControl.Items)
                {
                    if (item != this.DataContext) // Если это не наш блок данных
                    {
                        var container = itemsControl.ItemContainerGenerator.ContainerFromItem(item) as UIElement;
                        if (container != null)
                        {
                            container.Visibility = Visibility.Collapsed; // Скрываем стандартный блок
                        }
                    }
                }
            }
        }

        public void Render()
        {
            var _typeSymbol = _symbol as ITypeSymbol;
            var _methodSymbol = _symbol as IMethodSymbol;
            _textBlockSymbolName.Inlines.Add(_symbol, createLink: true);
            _imageSymbolName.Source = ResourceHelper.GetSource(_symbol.GetResourceForName());

            ApplySummary();
            ApplyParams();
            ApplyRuntimeValue();
            ApplyBaseTypes();
            ApplyErrorAndWarning();

            Render(_typeSymbol);
            Render(_methodSymbol);
        }

        public string GetSummary()
        {
            if (_commentXml == null)
                return null;

            return _commentXml.Element("summary")?.Value?.Trim();
        }

        public string GetParams()
        {
            if (_commentXml == null)
                return null;

            var elements = _commentXml.Elements("param").ToArray();
            List<string> listName = new List<string>();

            foreach (var element in elements)
            {
                var name = element.Attribute("name")?.Value?.Trim();
                var comment = element.Value?.Trim();
                if (!String.IsNullOrWhiteSpace(name) && !String.IsNullOrWhiteSpace(comment))
                {
                    listName.Add($"{name} - {comment}");
                }
            }

            return String.Join("\r\n", listName);
        }

        public void ApplyErrorAndWarning()
        {
            var listDiagnostic = _symbol.GetDiagnostics();
            var warnings = listDiagnostic.Where(d => d.Severity == DiagnosticSeverity.Warning).ToArray();
            var errors = listDiagnostic.Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
            if (warnings.Length > 0)
            {
                _imageWarning.Source = ResourceHelper.GetSource("GSharper.Resources.StatusWarning.png");
                _imageWarning.Visibility = Visibility.Visible;
                _textBoxWarning.Text = String.Join("\r\n", warnings.Select(x => x.GetMessage()));
                _textBoxWarning.Visibility = Visibility.Visible;
            }

            if (errors.Length > 0)
            {
                _imageError.Source = ResourceHelper.GetSource("GSharper.Resources.StatusError.png");
                _imageError.Visibility = Visibility.Visible;
                _textBoxError.Text = String.Join("\r\n", errors.Select(x => x.GetMessage()));
                _textBoxError.Visibility = Visibility.Visible;
            }
        }

        public void ApplySummary()
        {
            var summary = GetSummary();
            if(!String.IsNullOrWhiteSpace(summary))
            {
                _textBlockSummary.Inlines.AddLineIfNotEmpty();
                _textBlockSummary.Inlines.AddText(summary);
                _textBlockSummary.Visibility = Visibility.Visible;
            }   
        }

        public void ApplyParams()
        {
            var p = GetParams();
            if (!String.IsNullOrWhiteSpace(p))
            {
                _textBlockSummary.Inlines.AddLineIfNotEmpty();
                _textBlockSummary.Inlines.AddText(p);
                _textBlockSummary.Visibility = Visibility.Visible;
            }
        }

        public void ApplyRuntimeValue()
        {
            if (String.IsNullOrWhiteSpace(_expressionToEvaluate))
                return;

            if (Assistant.GetDte().Debugger.CurrentMode != dbgDebugMode.dbgBreakMode)
                return;

            var symbol = GetRuntimeType();
            if (symbol != null)
            {
                _textBlockSummary.Visibility = Visibility.Visible;
                _textBlockSummary.Inlines.AddLineIfNotEmpty();
                _textBlockSummary.Inlines.AddText("Runtime тип: ");
                _textBlockSummary.Inlines.Add(symbol, createLink: true);
            }
        }

        public void Render(ITypeSymbol _typeSymbol)
        {
            if (_typeSymbol == null)
                return;
        }

        public void Render(IMethodSymbol _methodSymbol)
        {
            if (_methodSymbol == null)
                return;
        }

        private void ApplyBaseTypes()
        {
            var typeSymbol = _symbol as ITypeSymbol;
            var methodSymbol = _symbol as IMethodSymbol;
            if (methodSymbol != null && (methodSymbol.MethodKind == MethodKind.Constructor || methodSymbol.MethodKind == MethodKind.StaticConstructor))
            {
                typeSymbol = methodSymbol.ContainingType;
            }

            if (typeSymbol != null)
            {
                var baseSymbols = typeSymbol.GetBaseSymbols().Where(x => !x.IsKeyword()).ToArray();
                var types = baseSymbols.Where(x => x.TypeKind != TypeKind.Interface).ToArray();
                var interfaces = baseSymbols.Where(x => x.TypeKind == TypeKind.Interface).ToArray();
                if(types.Length < 1 && interfaces.Length < 1)
                {
                    return;
                }
                _textBlockSummary.Visibility = Visibility.Visible;

                if (types.Length > 0)
                {
                    _textBlockSummary.Inlines.AddLineIfNotEmpty();
                    _textBlockSummary.Inlines.AddText("Базовые типы:", true);

                    for (int i = 0; i < types.Length; i++)
                    {
                        _textBlockSummary.Inlines.AddLineIfNotEmpty();
                        _textBlockSummary.Inlines.AddText("\t");
                        _textBlockSummary.Inlines.Add(types[i], false, false, createLink: true);
                    }
                }

                if (interfaces.Length > 0)
                {
                    _textBlockSummary.Inlines.AddLineIfNotEmpty();
                    _textBlockSummary.Inlines.AddText("Интерфейсы:", true);

                    for (int i = 0; i < interfaces.Length; i++)
                    {
                        _textBlockSummary.Inlines.AddLineIfNotEmpty();
                        _textBlockSummary.Inlines.AddText("\t");
                        _textBlockSummary.Inlines.Add(interfaces[i], false, false, createLink: true);
                    }
                }

                return;
            }

            if (methodSymbol != null)
            {
                var baseSymbols = methodSymbol.GetBaseSymbols().ToArray();
                if (baseSymbols.Length < 1)
                {
                    return;
                }

                _textBlockSummary.Visibility = Visibility.Visible;

                _textBlockSummary.Inlines.AddLineIfNotEmpty();
                _textBlockSummary.Inlines.AddText("Базовые вызовы:", true);

                for (int i = 0; i < baseSymbols.Length; i++)
                {
                    _textBlockSummary.Inlines.AddLineIfNotEmpty();
                    _textBlockSummary.Inlines.AddText("\t");
                    _textBlockSummary.Inlines.Add(baseSymbols[i], createLink: true, clearValue: false);
                }
            }    
        }

        private void OnClickSymbolName(object sender, RoutedEventArgs e)
        {
            CreateContextMenu(sender as Button, _symbol as IMethodSymbol);
        }

        private void OnClickCopy(object sender, RoutedEventArgs e)
        {

        }

        private void CreateContextMenu(Button button, IMethodSymbol symbol)
        {
            if (symbol == null || button == null)
                return;

            ContextMenu menu = new ContextMenu();
            menu.Add($"Перейти / найти реализации ", CommandMethodSymbol.GoToOrFindImplementations, OnClickContextMenu);
            menu.Add($"Перейти / найти базовые ", CommandMethodSymbol.GoToOrFindBase, OnClickContextMenu);

            // Показываем меню под кнопкой
            menu.PlacementTarget = button;
            menu.IsOpen = true;
        }

        private void OnClickContextMenu(object sender, RoutedEventArgs e)
        {
            OnClickContextMenu(sender as MenuItem);
            _session.DismissAsync();
        }

        private async void OnClickContextMenu(MenuItem menuItem)
        {
            if (_symbol is IMethodSymbol _methodSymbol && menuItem?.Tag is CommandMethodSymbol command)
            {
                if (command == CommandMethodSymbol.GoToOrFindImplementations)
                {
                    await _methodSymbol.TryGoToImplementations();
                }
            }
        }
    }
}
