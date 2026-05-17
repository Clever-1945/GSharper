using GSharper.Enums;
using GSharper.Extensions;
using GSharper.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

namespace GSharper.Controls
{
    /// <summary>
    /// Логика взаимодействия для TypeSymbolControl.xaml
    /// </summary>
    public partial class TypeSymbolControl : UserControl
    {
        private SymbolTypeFilter _symbolFilter = SymbolTypeFilter.Class;
        private SymbolTypeFilter[] symbols;
        private char _charChecked = '✔';
        private bool _isExternal = false;

        public Action<SymbolFilterModel> ChangedFilter { set; get; }

        public TypeSymbolControl()
        {
            InitializeComponent();
            symbols = default(SymbolTypeFilter).GetValues();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            applySelected();
            applyIsExternal();
        }

        public SymbolTypeFilter GetSymbolFilter() => _symbolFilter;

        public void ToLeft()
        {
            var first = symbols.First();
            if (first < _symbolFilter)
            {
                _symbolFilter--;
                ChangedFilter?.Invoke(new SymbolFilterModel() { 
                    SymbolType = _symbolFilter,
                    IsExternal = _isExternal
                });
                applySelected();
            }
        }

        public void ToRight()
        {
            var last = symbols.Last();
            if (_symbolFilter < last)
            {
                _symbolFilter++;
                ChangedFilter?.Invoke(new SymbolFilterModel()
                {
                    SymbolType = _symbolFilter,
                    IsExternal = _isExternal
                });
                applySelected();
            }
        }

        private void setCheckButton(Button button, bool isChecked)
        {
            var content = ((button.Content as string) ?? "").TrimStart(_charChecked).Trim();
            if (isChecked)
            {
                content = _charChecked.ToString() + " " + content;
            }
            button.Content = content;
        }

        private void applySelected()
        {
            var buttons = this.FindVisualChildren<Button>();
            foreach(var button in buttons)
            {
                var buttonSymbolFilter = (button.Tag as string).ToInt();
                if (buttonSymbolFilter != null)
                {
                    setCheckButton(button, buttonSymbolFilter.Value == (int)_symbolFilter);
                }
            }
        }

        private void OnClickFilter(object sender, MouseButtonEventArgs e)
        {
            var symbolFilter = (SymbolTypeFilter)((sender as Button).Tag.ToString().ToInt().Value);
            if (_symbolFilter != symbolFilter)
            {
                _symbolFilter = symbolFilter;
                ChangedFilter?.Invoke(new SymbolFilterModel()
                {
                    SymbolType = _symbolFilter,
                    IsExternal = _isExternal
                });
                applySelected();
            }
        }

        private void applyIsExternal()
        {
            setCheckButton(_buttonExternal, _isExternal);
        }

        private void OnChangeExternal(object sender, MouseButtonEventArgs e)
        {
            _isExternal = !_isExternal;
            applyIsExternal();
            ChangedFilter?.Invoke(new SymbolFilterModel()
            {
                SymbolType = _symbolFilter,
                IsExternal = _isExternal
            });
        }
    }
}
