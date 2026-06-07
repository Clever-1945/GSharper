using GSharper.Assistants;
using GSharper.Dialogs;
using GSharper.Extensions;
using GSharper.Helpers;
using GSharper.Models;
using Microsoft.CodeAnalysis;
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
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Linq;

namespace GSharper.Controls
{
    /// <summary>
    /// Логика взаимодействия для QuickInfoBlockControl.xaml
    /// </summary>
    public partial class QuickInfoBlockControl : UserControl
    {
        internal enum CommandSymbol
        {
            /// <summary>
            /// Копировать имя символа
            /// </summary>
            CopyName,
            
            /// <summary>
            /// Найти все реализации
            /// </summary>
            GoToOrFindImplementations,

            /// <summary>
            /// Найти базовые определения
            /// </summary>
            GoToOrFindBase,
        }

        internal class ContextMenuCommand
        {
            public CommandSymbol CommandSymbol { set; get; }
            public ISymbol Symbol { set; get; }
        }

        private ISymbol _symbol;
        private ISymbol _runtimeSymbol;
        private ISymbol[] _baseTypes;
        private ISymbol[] _implementations;
        private IMethodSymbol[] _overloadingMethods;
        private IMethodSymbol[] _extensions;
        private INamedTypeSymbol[] _extensionTypes;
        private Diagnostic[] _diagnostics;
        private SyntaxNode _node;
        private string _commentStringXml;
        private XElement _commentXml;
        private string _expressionToEvaluate;
        private string _expressionSelected;
        private IAsyncQuickInfoSession _session;
        private ContextMenu _symbolContextMenu = new ContextMenu();
        private bool _hideOther;
        private readonly int _limitCountSymbol = 4;
        private CancellationTokenSource _cancellationTokenSetData = null;
        private CancellationTokenSource _cancellationTokenExpressionSelected = null;

        public QuickInfoBlockControl()
        {
            InitializeComponent();
            // this.AddHandler(Hyperlink.ClickEvent, new RoutedEventHandler(OnHyperlinkClicked), true);
            this.AddHandler(Hyperlink.MouseDownEvent, new MouseButtonEventHandler(OnHyperlinkClicked), true);
            this.Loaded += OnLoaded;
            _progressBarLoading.Visibility = Visibility.Collapsed;
        }

        public QuickInfoBlockControl SetData(IAsyncQuickInfoSession session, ISymbol symbol, SyntaxNode node, bool hideOther = true)
        {
            if (_symbol == null && symbol == null)
                return this;

            _cancellationTokenSetData?.Cancel();
            _cancellationTokenSetData = new CancellationTokenSource();
            _progressBarLoading.Visibility = Visibility.Visible;

            ThreadPool.QueueUserWorkItem(async (s) => 
            {
                await SetDataAsync(session, symbol, node, hideOther);
            }, _cancellationTokenSetData.Token);

            return this;
        }

        public QuickInfoBlockControl SetExpressionSelected(string expressionSelected)
        {
            _expressionSelected = expressionSelected;

            _cancellationTokenExpressionSelected?.Cancel();
            _cancellationTokenExpressionSelected = new CancellationTokenSource();
            ThreadPool.QueueUserWorkItem(async (s) =>
            {
                var symbol = GetSymbolByExpression(_expressionSelected);
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                _textBlockSelectedType.Inlines.Clear();
                _textBlockSelectedType.Visibility = Visibility.Collapsed;
                if (symbol != null)
                {
                    _textBlockSelectedType.Inlines.AddText("Выдленый тип: ", isBold: true);
                    _textBlockSelectedType.Inlines.Add(symbol);
                    _textBlockSelectedType.Visibility = Visibility.Visible;
                }
            }, _cancellationTokenExpressionSelected.Token);

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
            _overloadingMethods = GetOverloadingMethods();
            _diagnostics = _symbol?.GetDiagnostics() ?? Array.Empty<Microsoft.CodeAnalysis.Diagnostic>();
            _extensions = _symbol?.GetMethodExtensions()?.ToArray() ?? Array.Empty<IMethodSymbol>();
            _extensionTypes = _extensions.Select(x => x.ContainingType).Distinct().ToArray();

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            Render();
        }

        private void OnHyperlinkClicked(object sender, MouseButtonEventArgs e)
        {
            var link = (e.OriginalSource as Hyperlink) ?? ((e.OriginalSource as Run)?.Parent as Hyperlink);
            if (link == null)
                return;

            if (e.ButtonState != MouseButtonState.Pressed)
                return;

            if (e.ChangedButton == MouseButton.Left)
            {
                if (link.Tag is ISymbol symbol)
                {
                    _progressBarLoading.Visibility = Visibility.Visible;
                    symbol.TryGoToDefinitionAsync()
                        .Then(x => {
                            _progressBarLoading.Visibility = Visibility.Collapsed;
                        })
                        .Catch(x => {
                            _progressBarLoading.Visibility = Visibility.Collapsed;
                        });
                }
                if (link.Tag is HyperlinkTagGoToSymbols goToSymbols)
                {
                    var dialog = new ListSymbolDialog(goToSymbols.Symbols);
                    dialog.Owner = Application.Current.MainWindow;
                    dialog.ShowModal();
                }
            }

            if (e.ChangedButton == MouseButton.Middle)
            {
                if (link.Tag is ISymbol symbol)
                {
                    this.SetData(_session, symbol, null, _hideOther);
                }
            }

            if (e.ChangedButton == MouseButton.Right)
            {
                if (link.Tag is ISymbol symbol)
                {
                    CreateContextMenu(link, symbol);
                }
            }
        }

        private ISymbol GetSymbolByExpression(string expression)
        {
            if (String.IsNullOrWhiteSpace(expression))
            {
                return null;
            }

            string expressionToEvaluate = $"{expression}.GetType().Namespace + \".\" + {expression}.GetType().Name";
            try
            {
                var eval = Assistant.GetDte().Debugger.GetExpression(expressionToEvaluate);
                if (!eval.IsValidValue)
                {
                    return null;
                }

                var typeName = eval.Value;
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

        private ISymbol GetRuntimeSymbol()
        {
            return GetSymbolByExpression(_expressionToEvaluate);
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
            _progressBarLoading.Visibility = Visibility.Collapsed;
            _textBlockSymbolName.Inlines.Clear();
            _textBlockSymbolName.Inlines.Add(_symbol, createLink: true);
            _imageSymbolName.Source = ResourceHelper.GetSource(_symbol.GetResourceForName());

            _textBlockSymbolName.Visibility = Visibility.Visible;
            _imageSymbolName.Visibility = Visibility.Visible;

            _textBlockSelectedType.Inlines.Clear();
            _textBlockSelectedType.Visibility = Visibility.Collapsed;

            ApplySummary();
            ApplyParams();
            ApplyRuntimeValue();
            ApplyBaseTypes();
            ApplyImplementations();
            ApplyErrorAndWarning();
            ApplyOverloadingMethods();
            ApplyExtensions();
            ApplyExtensionsTypes();
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

        private void ApplyOverloadingMethods()
        {
            _textBoxOverloadingMethodLabel.Visibility = Visibility.Collapsed;
            _textBoxOverloadingMethodLabel.Text = "";
            _scrollViewerOverloadingMethod.Visibility = Visibility.Collapsed;
            _textBoxOverloadingMethod.Visibility = Visibility.Collapsed;
            _textBoxOverloadingMethod.Inlines.Clear();

            if (_overloadingMethods.Length < 1)
                return;

            _textBoxOverloadingMethodLabel.Visibility = Visibility.Visible;
            _scrollViewerOverloadingMethod.Visibility = Visibility.Visible;
            _textBoxOverloadingMethod.Visibility = Visibility.Visible;

            _textBoxOverloadingMethodLabel.Text = "Перегрузки:";

            if (_overloadingMethods.Length > 10)
            {
                _textBoxOverloadingMethod.Inlines.AddText("\t");
                _textBoxOverloadingMethod.Inlines.AddDots(_overloadingMethods);
                _textBoxOverloadingMethod.Inlines.AddText("\r\n");
            }

            for (int i = 0; i < _overloadingMethods.Length; i++) 
            {
                var overloading = _overloadingMethods[i];
                if (i > 0)
                {
                    _textBoxOverloadingMethod.Inlines.AddText("\r\n");
                }
                _textBoxOverloadingMethod.Inlines.AddText("\t");
                _textBoxOverloadingMethod.Inlines.Add(overloading, createLink: true, clearValue: false);
            }
        }

        private void ApplyExtensionsTypes()
        {
            _textBlockExtensionsTypes.Inlines.Clear();
            _textBlockExtensionsTypes.Visibility = Visibility.Collapsed;
            if (_extensionTypes.Length < 1)
            {
                return;
            }

            _textBlockExtensionsTypes.Visibility = Visibility.Visible;

            _textBlockExtensionsTypes.Inlines.AddText("Классы расширения: ", true);
            for (int i = 0; i < Math.Min(_extensionTypes.Length, 4); i++)
            {
                var extension = _extensionTypes[i];
                _textBlockExtensionsTypes.Inlines.AddText("\r\n");
                _textBlockExtensionsTypes.Inlines.AddText("\t");
                _textBlockExtensionsTypes.Inlines.Add(extension, createLink: true, clearValue: false);
            }

            if (_extensionTypes.Length > 4)
            {
                _textBlockExtensionsTypes.Inlines.AddText("\r\n");
                _textBlockExtensionsTypes.Inlines.AddText("\t");
                _textBlockExtensionsTypes.Inlines.AddDots(_extensionTypes);
            }
        }

        private void ApplyExtensions()
        {
            _textBlockExtensions.Inlines.Clear();
            _textBlockExtensions.Visibility = Visibility.Collapsed;
            if (_extensions.Length < 1)
            {
                return;
            }
            _textBlockExtensions.Visibility = Visibility.Visible;

            _textBlockExtensions.Inlines.AddText("Методы расширения: ", true);
            for (int i = 0; i < Math.Min(_extensions.Length, 4); i++) 
            {
                var extension = _extensions[i];
                _textBlockExtensions.Inlines.AddText("\r\n");
                _textBlockExtensions.Inlines.AddText("\t");
                _textBlockExtensions.Inlines.Add(extension, createLink: true, clearValue: false);
            }

            if (_extensions.Length > 4)
            {
                _textBlockExtensions.Inlines.AddText("\r\n");
                _textBlockExtensions.Inlines.AddText("\t");
                _textBlockExtensions.Inlines.AddDots(_extensions);
            }
        }

        private void ApplyErrorAndWarning()
        {
            var warnings = _diagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Warning).ToArray();
            var errors = _diagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error).ToArray();

            _textBoxError.Visibility = Visibility.Collapsed;
            _textBoxWarning.Visibility = Visibility.Collapsed;

            _imageError.Visibility = Visibility.Collapsed;
            _imageError.Source = null;

            _imageWarning.Visibility = Visibility.Collapsed;
            _imageWarning.Source = null;

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
                _textBoxSummary.Text = summary;
                _textBoxSummary.Visibility = Visibility.Visible;
            }
            else
            {
                _textBoxSummary.Text = "";
                _textBoxSummary.Visibility = Visibility.Collapsed;
            }
        }

        private void ApplyParams()
        {
            var p = GetParams();
            if (!String.IsNullOrWhiteSpace(p))
            {
                _textBoxParams.Text = p;
                _textBoxParams.Visibility = Visibility.Visible;
            }
            else
            {
                _textBoxParams.Visibility = Visibility.Collapsed;
                _textBoxParams.Text = "";
            }
        }

        private void ApplyRuntimeValue()
        {
            _textBlockRuntimeValue.Visibility = Visibility.Collapsed;
            _textBlockRuntimeValue.Inlines.Clear();
            if (_runtimeSymbol != null)
            {
                _textBlockRuntimeValue.Visibility = Visibility.Visible;
                _textBlockRuntimeValue.Inlines.AddText("Runtime тип: ", isBold: true);
                _textBlockRuntimeValue.Inlines.Add(_runtimeSymbol, createLink: true);
            }
        }

        private ISymbol[] GetBaseTypes()
        {
            return _symbol.GetBaseSymbols().Where(x => !x.IsKeyword()).ToArray();
        }

        private void ApplyBaseTypes()
        {
            var interfaces = _baseTypes.Select(x => x as ITypeSymbol).Where(x => x?.TypeKind == TypeKind.Interface).ToArray();
            var nointerfaces = _baseTypes.Where(x => (x as ITypeSymbol)?.TypeKind != TypeKind.Interface).ToArray();

            _textBlockBaseTypes.Inlines.Clear();
            _textBlockBaseTypes.Visibility = Visibility.Collapsed;

            if (nointerfaces?.Length > 0)
            {
                _textBlockBaseTypes.Visibility = Visibility.Visible;
                _textBlockBaseTypes.Inlines.AddLineIfNotEmpty();
                _textBlockBaseTypes.Inlines.AddText("Базовые типы:", true);

                for (int i = 0; i < nointerfaces.Length && i < _limitCountSymbol; i++)
                {
                    var typeSymbol = nointerfaces[i] as ITypeSymbol;
                    var methodSymbol = nointerfaces[i] as IMethodSymbol;
                    var propertySymbol = nointerfaces[i] as IPropertySymbol;
                    _textBlockBaseTypes.Inlines.AddLineIfNotEmpty();
                    _textBlockBaseTypes.Inlines.AddText("\t");

                    if (typeSymbol != null)
                    {
                        _textBlockBaseTypes.Inlines.Add(typeSymbol, false, false, createLink: true);
                    }
                    else if (methodSymbol != null)
                    {
                        _textBlockBaseTypes.Inlines.Add(methodSymbol, createLink: true, clearValue: false);
                    }
                    else if (propertySymbol != null)
                    {
                        _textBlockBaseTypes.Inlines.Add(propertySymbol, createLink: true, clearValue: false);
                    }
                }

                if (nointerfaces.Length >= _limitCountSymbol)
                {
                    _textBlockBaseTypes.Inlines.AddLineIfNotEmpty();
                    _textBlockBaseTypes.Inlines.AddText("\t");
                    _textBlockBaseTypes.Inlines.AddDots(nointerfaces);
                }
            }

            if (interfaces.Length > 0)
            {
                _textBlockBaseTypes.Visibility = Visibility.Visible;
                _textBlockBaseTypes.Inlines.AddLineIfNotEmpty();
                _textBlockBaseTypes.Inlines.AddText("Интерфейсы:", true);

                for (int i = 0; i < interfaces.Length && i < _limitCountSymbol; i++)
                {
                    _textBlockBaseTypes.Inlines.AddLineIfNotEmpty();
                    _textBlockBaseTypes.Inlines.AddText("\t");
                    _textBlockBaseTypes.Inlines.Add(interfaces[i], false, false, createLink: true);
                }

                if (interfaces.Length >= _limitCountSymbol)
                {
                    _textBlockBaseTypes.Inlines.AddLineIfNotEmpty();
                    _textBlockBaseTypes.Inlines.AddText("\t");
                    _textBlockBaseTypes.Inlines.AddDots(interfaces);
                }
            }
        }

        private IMethodSymbol[] GetOverloadingMethods()
        {
            return (_symbol as IMethodSymbol)?.GetOverloadingMethods()?.ToArray() ?? Array.Empty<IMethodSymbol>();
        }

        private ISymbol[] GetImplementations()
        {
            return _symbol.GetImplementations().ToArray();
        }

        private void ApplyImplementations()
        {
            _textBlockImplementations.Visibility = Visibility.Collapsed;
            _textBlockImplementations.Inlines.Clear();

            if (_implementations?.Length > 0)
            {
                _textBlockImplementations.Visibility = Visibility.Visible;
                _textBlockImplementations.Inlines.AddLineIfNotEmpty();
                _textBlockImplementations.Inlines.AddText("Реализации:", true);

                for (int i = 0; i < _implementations.Length && i < _limitCountSymbol; i++)
                {
                    _textBlockImplementations.Inlines.AddLineIfNotEmpty();
                    _textBlockImplementations.Inlines.AddText("\t");
                    var typeSymbol = _implementations[i] as ITypeSymbol;
                    var methodSymbol = _implementations[i] as IMethodSymbol;
                    var propertySymbol = _implementations[i] as IPropertySymbol;

                    if (typeSymbol != null)
                    {
                        _textBlockImplementations.Inlines.Add(typeSymbol, createLink: true, clearValue: false);
                    }
                    else if(methodSymbol != null)
                    {
                        _textBlockImplementations.Inlines.Add(methodSymbol, createLink: true, clearValue: false);
                    }
                    else if (propertySymbol != null)
                    {
                        _textBlockImplementations.Inlines.Add(propertySymbol, createLink: true, clearValue: false);
                    }
                }

                if (_implementations.Length >= _limitCountSymbol)
                {
                    _textBlockImplementations.Inlines.AddLineIfNotEmpty();
                    _textBlockImplementations.Inlines.AddText("\t");
                    _textBlockImplementations.Inlines.AddDots(_implementations);
                }
            }
        }

        private void OnClickSymbolName(object sender, RoutedEventArgs e)
        {
            CreateContextMenu(sender as Button, _symbol);
        }

        private void OnClickCopy(object sender, RoutedEventArgs e)
        {

        }

        private void CreateContextMenu(DependencyObject target, ISymbol symbol)
        {
            if (symbol == null)
                return;

            _symbolContextMenu.Items.Clear();
            
            _symbolContextMenu.Add($"Копировать имя", new ContextMenuCommand()
            {
                CommandSymbol = CommandSymbol.CopyName,
                Symbol = symbol
            }, OnClickContextMenu);


            _symbolContextMenu.Add($"Перейти / найти реализации", new ContextMenuCommand()
            {
                CommandSymbol = CommandSymbol.GoToOrFindImplementations,
                Symbol = symbol
            }, OnClickContextMenu);


            _symbolContextMenu.Add($"Перейти / найти базовые", new ContextMenuCommand()
            {
                CommandSymbol = CommandSymbol.GoToOrFindBase,
                Symbol = symbol
            }, OnClickContextMenu);

            // Показываем меню под кнопкой
            // menu.PlacementTarget = button;
            _symbolContextMenu.PlacementTarget = this;
            _symbolContextMenu.IsOpen = true;

            if (target != null)
            {
                ContextMenuService.SetContextMenu(target, _symbolContextMenu);
            }
        }

        private void OnClickContextMenu(object sender, RoutedEventArgs e)
        {
            OnClickContextMenu(sender as MenuItem);
            _session?.DismissAsync();
        }

        private async void OnClickContextMenu(MenuItem menuItem)
        {
            if (menuItem?.Tag is ContextMenuCommand command)
            {
                if (command.CommandSymbol == CommandSymbol.GoToOrFindImplementations)
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    _progressBarLoading.Visibility = Visibility.Visible;
                    await command.Symbol.TryGoToImplementations();

                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    _progressBarLoading.Visibility = Visibility.Collapsed;
                }

                if (command.CommandSymbol == CommandSymbol.GoToOrFindBase)
                {
                    // await command.Symbol.TryGoToImplementations();
                }

                if (command.CommandSymbol == CommandSymbol.CopyName)
                {
                    Clipboard.SetText(command.Symbol.Name);
                }
            }
        }
    }
}
