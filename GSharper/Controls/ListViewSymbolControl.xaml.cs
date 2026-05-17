using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using GSharper.Extensions;
using GSharper.Interfaces;
using GSharper.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace GSharper.Controls
{
    /// <summary>
    /// Логика взаимодействия для ListViewSymbolControl.xaml
    /// </summary>
    public partial class ListViewSymbolControl : UserControl, IAsyncControl
    {
        private SymbolModel[] _symbols;
        private ISearchPattern _searchPattern;

        public ProgressBar ProgressBarFilter => _progressBarFilter;
        public TextBlock TextBlockInfo => _textBlockInfo;

        public int CountFilter { set; get; }
        private long filterId;

        public Action<SymbolModel> OnActive { set; get; }
        public Action OnLeft { set; get; }
        public Action OnRight { set; get; }
        public Action OnEscape { set; get; }

        public ListViewSymbolControl()
        {
            InitializeComponent();
            this.Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _textBoxSearch.Focus();
            ApplyFilter();
        }

        public static readonly DependencyProperty SymbolsProperty = DependencyProperty.Register(
            "Symbols",
            typeof(SymbolModel[]),
            typeof(ListItemSymbolControl),
            new PropertyMetadata(default(SymbolModel[]), SetSymbols));

        public static readonly DependencyProperty SearchPatternProperty = DependencyProperty.Register(
            "SearchPattern",
            typeof(ISearchPattern),
            typeof(ListItemSymbolControl),
            new PropertyMetadata(default(ISearchPattern), SetSearchPattern));

        public SymbolModel[] Symbols
        {
            get { return (SymbolModel[])GetValue(SymbolsProperty); }
            set 
            { 
                SetValue(SymbolsProperty, value);
                SetSymbols(value ?? Array.Empty<SymbolModel>());
            }
        }

        public ISearchPattern SearchPattern
        {
            get { return (ISearchPattern)GetValue(SearchPatternProperty); }
            set 
            { 
                SetValue(SearchPatternProperty, value);
                SetSearchPattern(value);
            }
        }

        private static void SetSearchPattern(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is ISearchPattern searchPattern)
            {
                if (d is ListViewSymbolControl control)
                {
                    control.SetSearchPattern(searchPattern);
                }
            }
        }

        public void SetSearchPattern(ISearchPattern searchPattern)
        {
            _searchPattern = searchPattern;
            ApplyFilter();
        }

        private static void SetSymbols(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is SymbolModel[] symbols)
            {
                if (d is ListViewSymbolControl control)
                {
                    control.SetSymbols(symbols);
                }
            }
        }

        public void SetSymbols(SymbolModel[] symbols)
        {
            _symbols = symbols;
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            _searchPattern?.SetSearchText(_textBoxSearch.Text ?? "");
            this.StartAsync(async () => await ApplyFilterAsync());
        }

        private async Task ApplyFilterAsync()
        {
            var currentFilterId = Interlocked.Add(ref filterId, 1);
            if (currentFilterId != filterId)
                return;

            var symbols = _symbols ?? Array.Empty<SymbolModel>();

            foreach ( var symbol in symbols)
            {
                if (currentFilterId != filterId)
                    return;
                symbol.Weight = _searchPattern?.IsEquals(symbol) ?? 0;
            }

            if (currentFilterId != filterId)
                return;
            symbols = symbols.Where(x => x.Weight >= 0).OrderByDescending(x => x.Weight).ToArray();

            if (currentFilterId != filterId)
                return;

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            _listViewSymbols.ItemsSource = symbols;
        }

        private void ActiveSelected()
        {
            ThreadPool.QueueUserWorkItem((s) =>
            {
                Thread.Sleep(50);
                Dispatcher.Invoke(() => 
                {
                    var symbol = _listViewSymbols.SelectedValue as SymbolModel;
                    if (symbol != null)
                    {
                        OnActive?.Invoke(symbol);
                    }
                });
            });
        }

        private void OnMouseSelected(object sender, MouseButtonEventArgs e)
        {
            ActiveSelected();
        }

        private void OnLostFocusSearch(object sender, RoutedEventArgs e)
        {
            ThreadPool.QueueUserWorkItem((x) =>
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (!_textBoxSearch.IsFocused)
                    {
                        _textBoxSearch.Focus();
                    }
                }), System.Windows.Threading.DispatcherPriority.Input);
            });
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.IsRepeat)
                return;
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                OnEscape?.Invoke();
            }
            else if (e.Key == System.Windows.Input.Key.Down)
            {
                SetDownSelected();
            }
            else if (e.Key == System.Windows.Input.Key.Up)
            {
                SetUpSelected();
            }
            else if (e.Key == System.Windows.Input.Key.Enter)
            {
                ActiveSelected();
            }
            else
            {
                Key pressedKey = (e.Key == Key.System) ? e.SystemKey : e.Key;

                if (Keyboard.Modifiers == ModifierKeys.Alt && pressedKey == Key.Left)
                {
                    OnLeft?.Invoke();
                }
                else if (Keyboard.Modifiers == ModifierKeys.Alt && pressedKey == Key.Right)
                {
                    OnRight?.Invoke();
                }
            }
        }

        private void SetSelected(int addedIndex)
        {
            var symbols = _listViewSymbols.ItemsSource as SymbolModel[];
            if (symbols == null || symbols.Length < 1)
            {
                _listViewSymbols.SelectedValue = null;
                return;
            }

            var symbol = _listViewSymbols.SelectedValue as SymbolModel;
            if (symbol == null)
            {
                _listViewSymbols.SelectedValue = symbols.FirstOrDefault();
                _listViewSymbols.ScrollIntoView(_listViewSymbols.SelectedValue);
                return;
            }
            var index = symbols.FindIndex(x => x == symbol);
            if (index < 0)
            {
                _listViewSymbols.SelectedValue = symbols.FirstOrDefault();
                _listViewSymbols.ScrollIntoView(_listViewSymbols.SelectedValue);
                return;
            }
            var nextIndex = index + addedIndex;
            if (nextIndex >= 0 && nextIndex < symbols.Length)
            {
                _listViewSymbols.SelectedValue = symbols[nextIndex];
            }
            else
            {
                if (nextIndex < 0)
                {
                    _listViewSymbols.SelectedValue = symbols[0];
                }
                else
                {
                    _listViewSymbols.SelectedValue = symbols.LastOrDefault();
                }
            }
            _listViewSymbols.ScrollIntoView(_listViewSymbols.SelectedValue);
        }

        private void SetDownSelected()
        {
            SetSelected(1);
        }

        private void SetUpSelected()
        {
            SetSelected(-1);
        }

        private void OnChangedText(object sender, TextChangedEventArgs e)
        {
            ApplyFilter();
        }

        public SymbolModel GetSelected()
        {
            return _listViewSymbols.SelectedValue as SymbolModel;
        }
    }
}
