using EnvDTE;
using GSharper.Dialogs;
using GSharper.Extensions;
using GSharper.Helpers;
using GSharper.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Elfie.Model;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
        private ISymbol _runtimeSymbol;
        private ISymbol[] _baseTypes;
        private ISymbol[] _implementations;
        private SyntaxNode _node;
        private string _commentStringXml;
        private XElement _commentXml;
        private string _expressionToEvaluate;
        private IAsyncQuickInfoSession _session;
        private bool _hideOther;
        private readonly int _limitCountSymbol = 4;
        private CancellationTokenSource _cancellationTokenSetData = null;

        public QuickInfoBlockControl()
        {
            InitializeComponent();
            this.AddHandler(Hyperlink.ClickEvent, new RoutedEventHandler(OnHyperlinkClicked), true);
            this.Loaded += OnLoaded;
        }

        public QuickInfoBlockControl SetData(IAsyncQuickInfoSession session, ISymbol symbol, SyntaxNode node, bool hideOther = true)
        {
            _cancellationTokenSetData?.Cancel();
            _cancellationTokenSetData = new CancellationTokenSource();

            ThreadPool.QueueUserWorkItem(async (s) => 
            {
                await SetDataAsync(session, symbol, node, hideOther);
            }, _cancellationTokenSetData.Token);

            return this;
        }

        private async Task SetDataAsync(IAsyncQuickInfoSession session, ISymbol symbol, SyntaxNode node, bool hideOther = true)
        {
            _hideOther = hideOther;
            _symbol = symbol;
            _node = node;
            _session = session;
            _commentStringXml = _symbol?.GetDocumentationCommentXml();
            _commentXml = CommentXml();
            _expressionToEvaluate = GetExpressionToEvaluate();
            _runtimeSymbol = GetRuntimeSymbol();
            _baseTypes = GetBaseTypes();
            _implementations = GetImplementations();

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            Render();
        }

        private void OnHyperlinkClicked(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is Hyperlink link)
            {
                if (link.Tag is ISymbol symbol)
                {
                    symbol.TryGoToDefinitionAsync();
                }
                if (link.Tag is HyperlinkTagGoToSymbols goToSymbols)
                {
                    var dialog = new ListSymbolDialog(goToSymbols.Symbols);
                    dialog.Owner = Application.Current.MainWindow;
                    dialog.ShowModal();
                }
            }
        }

        private ISymbol GetRuntimeSymbol()
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
            if (_hideOther)
            {
                ClearStandart();
            }
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
                    if (item != this.DataContext)
                    {
                        var container = itemsControl.ItemContainerGenerator.ContainerFromItem(item) as UIElement;
                        if (container != null)
                        {
                            container.Visibility = Visibility.Collapsed;
                        }
                    }
                }
            }
        }

        private void Render()
        {
            var _typeSymbol = _symbol as ITypeSymbol;
            var _methodSymbol = _symbol as IMethodSymbol;
            _textBlockSymbolName.Inlines.Clear();
            _textBlockSummary.Inlines.Clear();
            _textBoxError.Visibility = Visibility.Collapsed;
            _textBoxWarning.Visibility = Visibility.Collapsed;
            _imageError.Visibility = Visibility.Collapsed;
            _imageWarning.Visibility = Visibility.Collapsed;

            _textBlockSymbolName.Inlines.Add(_symbol, createLink: true);
            _imageSymbolName.Source = ResourceHelper.GetSource(_symbol.GetResourceForName());

            ApplySummary();
            ApplyParams();
            ApplyRuntimeValue();
            ApplyBaseTypes();
            ApplyImplementations();
            ApplyErrorAndWarning();
        }

        private string GetSummary()
        {
            if (_commentXml == null)
                return null;

            var summary = _commentXml.Element("summary")?.Value?.Trim();
            var remarks = _commentXml.Element("remarks")?.Value?.Trim();

            return String.Join("\r\n", new string[] 
            {
                summary,
                remarks
            }.Where(x => !String.IsNullOrWhiteSpace(x)));
        }

        private string GetParams()
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

        private void ApplyErrorAndWarning()
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

        private void ApplySummary()
        {
            var summary = GetSummary();
            if(!String.IsNullOrWhiteSpace(summary))
            {
                _textBlockSummary.Inlines.AddLineIfNotEmpty();
                _textBlockSummary.Inlines.AddText(summary);
                _textBlockSummary.Visibility = Visibility.Visible;
            }   
        }

        private void ApplyParams()
        {
            var p = GetParams();
            if (!String.IsNullOrWhiteSpace(p))
            {
                _textBlockSummary.Inlines.AddLineIfNotEmpty();
                _textBlockSummary.Inlines.AddText(p);
                _textBlockSummary.Visibility = Visibility.Visible;
            }
        }

        private void ApplyRuntimeValue()
        {
            if (_runtimeSymbol != null)
            {
                _textBlockSummary.Visibility = Visibility.Visible;
                _textBlockSummary.Inlines.AddLineIfNotEmpty();
                _textBlockSummary.Inlines.AddText("Runtime тип: ");
                _textBlockSummary.Inlines.Add(_runtimeSymbol, createLink: true);
            }
        }

        private ISymbol[] GetBaseTypes()
        {
            var typeSymbol = _symbol as ITypeSymbol;
            var methodSymbol = _symbol as IMethodSymbol;
            if (methodSymbol != null && (methodSymbol.MethodKind == MethodKind.Constructor || methodSymbol.MethodKind == MethodKind.StaticConstructor))
            {
                typeSymbol = methodSymbol.ContainingType;
            }

            if (typeSymbol != null)
            {
                return typeSymbol.GetBaseSymbols().Where(x => !x.IsKeyword()).ToArray();
            }

            if (methodSymbol != null)
            {
                return methodSymbol.GetBaseSymbols().ToArray();
            }

            return Array.Empty<ISymbol>();
        }

        private void ApplyBaseTypes()
        {
            var types = _baseTypes.Where(x => (x as ITypeSymbol)?.TypeKind != TypeKind.Interface).ToArray();
            var interfaces = _baseTypes.Select(x => x as ITypeSymbol).Where(x => x?.TypeKind == TypeKind.Interface).ToArray();

            if (types.Length > 0)
            {
                _textBlockSummary.Visibility = Visibility.Visible;
                _textBlockSummary.Inlines.AddLineIfNotEmpty();
                _textBlockSummary.Inlines.AddText("Базовые типы:", true);

                for (int i = 0; i < types.Length && i < _limitCountSymbol; i++)
                {
                    _textBlockSummary.Inlines.AddLineIfNotEmpty();
                    _textBlockSummary.Inlines.AddText("\t");
                    var typeSymbol = types[i] as ITypeSymbol;
                    var methodSymbol = types[i] as IMethodSymbol;
                    if (typeSymbol != null)
                    {
                        _textBlockSummary.Inlines.Add(typeSymbol, false, false, createLink: true);
                    }
                    else if (methodSymbol != null)
                    {
                        _textBlockSummary.Inlines.Add(methodSymbol, createLink: true, clearValue: false);
                    }
                }
                if (types.Length >= _limitCountSymbol)
                {
                    _textBlockSummary.Inlines.AddLineIfNotEmpty();
                    _textBlockSummary.Inlines.AddText("\t");
                    _textBlockSummary.Inlines.AddDots(types);
                }
            }

            if (interfaces.Length > 0)
            {
                _textBlockSummary.Visibility = Visibility.Visible;
                _textBlockSummary.Inlines.AddLineIfNotEmpty();
                _textBlockSummary.Inlines.AddText("Интерфейсы:", true);

                for (int i = 0; i < interfaces.Length && i < _limitCountSymbol; i++)
                {
                    _textBlockSummary.Inlines.AddLineIfNotEmpty();
                    _textBlockSummary.Inlines.AddText("\t");
                    _textBlockSummary.Inlines.Add(interfaces[i], false, false, createLink: true);
                }

                if (types.Length >= _limitCountSymbol)
                {
                    _textBlockSummary.Inlines.AddLineIfNotEmpty();
                    _textBlockSummary.Inlines.AddText("\t");
                    _textBlockSummary.Inlines.AddDots(interfaces);
                }
            }
        }

        private ISymbol[] GetImplementations()
        {
            var typeSymbol = _symbol as ITypeSymbol;
            var methodSymbol = _symbol as IMethodSymbol;
            if (methodSymbol != null && (methodSymbol.MethodKind == MethodKind.Constructor || methodSymbol.MethodKind == MethodKind.StaticConstructor))
            {
                typeSymbol = methodSymbol.ContainingType;
            }

            if (typeSymbol != null)
            {
                return typeSymbol.GetImplementations().ToArray();
            }

            if (methodSymbol != null)
            {
                return methodSymbol.GetImplementations().ToArray();
            }

            return Array.Empty<ISymbol>();
        }

        private void ApplyImplementations()
        {
            var methods = _implementations.Select(x => x as IMethodSymbol).Where(x => x != null).ToArray();
            var types = _implementations.Select(x => x as ITypeSymbol).Where(x => x != null).ToArray();

            if (types.Length > 0)
            {
                _textBlockSummary.Visibility = Visibility.Visible;
                _textBlockSummary.Inlines.AddLineIfNotEmpty();
                _textBlockSummary.Inlines.AddText("Реализации:", true);

                for (int i = 0; i < types.Length && i < _limitCountSymbol; i++)
                {
                    _textBlockSummary.Inlines.AddLineIfNotEmpty();
                    _textBlockSummary.Inlines.AddText("\t");
                    _textBlockSummary.Inlines.Add(types[i], createLink: true, clearValue: false);
                }

                if (types.Length >= _limitCountSymbol)
                {
                    _textBlockSummary.Inlines.AddLineIfNotEmpty();
                    _textBlockSummary.Inlines.AddText("\t");
                    _textBlockSummary.Inlines.AddDots(types);
                }
            }
            if (methods.Length > 0)
            {
                _textBlockSummary.Visibility = Visibility.Visible;
                _textBlockSummary.Inlines.AddLineIfNotEmpty();
                _textBlockSummary.Inlines.AddText("Реализации:", true);

                for (int i = 0; i < methods.Length && i < _limitCountSymbol; i++)
                {
                    _textBlockSummary.Inlines.AddLineIfNotEmpty();
                    _textBlockSummary.Inlines.AddText("\t");
                    _textBlockSummary.Inlines.Add(methods[i], createLink: true, clearValue: false);
                }

                if (methods.Length >= _limitCountSymbol)
                {
                    _textBlockSummary.Inlines.AddLineIfNotEmpty();
                    _textBlockSummary.Inlines.AddText("\t");
                    _textBlockSummary.Inlines.AddDots(methods);
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
            _session?.DismissAsync();
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
