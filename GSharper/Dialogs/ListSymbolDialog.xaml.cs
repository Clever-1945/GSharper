using GSharper.Assistants;
using GSharper.Helpers;
using GSharper.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Elfie.Model;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
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
using System.Windows.Shapes;

namespace GSharper.Dialogs
{
    /// <summary>
    /// Логика взаимодействия для ListSymbolDialog.xaml
    /// </summary>
    public partial class ListSymbolDialog : Window
    {
        private ISymbol[] _symbols;

        public ListSymbolDialog(ISymbol[] symbols)
        {
            InitializeComponent();
            _symbols = symbols;
            this.Loaded += OnLoaded;
            this.Deactivated += OnDeactivated;
            _listViewSymbolControl.OnEscape = () => this.Hide();
            _listViewSymbolControl.OnActive = (s) => this.GoToSymbol();
            _listViewSymbolControl.SetSearchPattern(new SearchSymbolPattern());
        }

        private void OnDeactivated(object sender, EventArgs e)
        {
            this.Close();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            var symbols = _symbols.Select(x => new SymbolModel(x)).ToArray();
            _listViewSymbolControl.SetSymbols(symbols);
        }

        private async Task GoToSymbol()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var symbol = _listViewSymbolControl.GetSelected();
            if (symbol == null)
                return;

            if (symbol.ProjectItem != null)
            {
                EnvDTE.Window window = symbol.ProjectItem.Open(EnvDTE.Constants.vsViewKindPrimary);
                window.Activate();
                this.Hide();
                return;
            }

            var location = symbol.Symbol.Locations.FirstOrDefault(loc => loc.IsInSource);
            if (location != null)
            {
                var document = Assistant.GetWorkspace().CurrentSolution.GetDocument(location.SourceTree);
                await Assistant.GetWorkspace().TryGoToDefinitionAsync(symbol.Symbol, document.Project, default);
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                this.Hide();
            }
            else
            {
                var project = Assistant.GetWorkspace().CurrentSolution.Projects.FirstOrDefault();
                await Assistant.GetWorkspace().TryGoToDefinitionAsync(symbol.Symbol, project, default);
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                this.Hide();
            }
        }
    }
}
