using EnvDTE;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Formatting;
using GSharper.Extensions;
using GSharper.Helpers;
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
using static Microsoft.VisualStudio.Shell.ThreadedWaitDialogHelper;

namespace GSharper.Controls
{
    /// <summary>
    /// Элемент списка символов
    /// </summary>
    public partial class ListItemSymbolControl : UserControl
    {
        private SymbolModel _symbol;

        public ListItemSymbolControl()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty SymbolInstanceProperty = DependencyProperty.Register(
            "SymbolInstance",
            typeof(SymbolModel),
            typeof(ListItemSymbolControl),
            new PropertyMetadata(default(SymbolModel), SetSymbolInstance));

        public SymbolModel SymbolInstance
        {
            get { return (SymbolModel)GetValue(SymbolInstanceProperty); }
            set { SetValue(SymbolInstanceProperty, value); }
        }

        private static void SetSymbolInstance(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is SymbolModel symbol)
            {
                if (d is ListItemSymbolControl control)
                {
                    control.SetSymbol(symbol);
                }
            }
        }

        public void SetSymbol(SymbolModel symbol)
        {
            _symbol = symbol;

            ApplyAssembly(_symbol?.TypeSymbol);
            ApplyAssembly(_symbol?.MethodSymbol);

            ApplyImage(_symbol?.Symbol);

            ApplyName(_symbol?.ProjectItem);
            ApplyName(_symbol?.Symbol);
        }

        private void ApplyName(ProjectItem projectItem)
        {
            if (projectItem != null)
            {
                var parents = projectItem.GetParents().Select(x => x.Name).ToList();
                parents.Reverse();
                parents.Add(projectItem.Name);
                _textBlockName.Text = String.Join("/", parents.Where(x => !String.IsNullOrWhiteSpace(x)));
            }
        }

        private void ApplyName(ISymbol symbol, bool setNameSpace = true, bool clearValue = true)
        {
            if (symbol is ITypeSymbol typeSymbol)
            {
                _textBlockName.Inlines.Add(typeSymbol, setNameSpace, clearValue, createLink: false);
            }
            else if (symbol != null)
            {
                _textBlockName.Inlines.Add(symbol, createLink: false);
            }
        }

        private void ApplyAssembly(INamedTypeSymbol namedTypeSymbol)
        {
            if (namedTypeSymbol != null)
            {
                _textBlockAssembly.Text = namedTypeSymbol.ContainingAssembly.Name;
            }
        }

        private void ApplyAssembly(IMethodSymbol methodSymbol)
        {
            if (methodSymbol != null)
            {
                _textBlockAssembly.Text = methodSymbol.ContainingAssembly.Name;
            }
        }

        private void ApplyImage(ISymbol namedTypeSymbol)
        {
            _imageElement.Source = ResourceHelper.GetSource(_symbol.Symbol.GetResourceForName());
        }

        public SymbolModel GetSymbol()
        {
            return _symbol;
        }
    }
}
