using EnvDTE;
using EnvDTE80;
using GSharper.Enums;
using GSharper.Extensions;
using GSharper.Helpers;
using GSharper.Interfaces;
using GSharper.Models;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace GSharper.Dialogs
{
    /// <summary>
    /// Логика взаимодействия для SearchDialog.xaml
    /// </summary>
    public partial class SearchDialog : System.Windows.Window
    {
        private SymbolFilterModel _symbolFilterModel;

        private DTE2 _dte;
        private IComponentModel _componentModel;
        private VisualStudioWorkspace _workspace;
        private CancellationTokenSource _cancellationTokenSource = null;
        private ulong _searchId = 0;

        public SearchDialog()
        {
            InitializeComponent();

            _dte = (Package.GetGlobalService(typeof(DTE)) as DTE2);
            _componentModel = (Package.GetGlobalService(typeof(SComponentModel)) as IComponentModel);
            _workspace = _componentModel.GetService<VisualStudioWorkspace>();
            this.Loaded += OnLoaded;
            this.Activated += OnActivated;
            this.Deactivated += OnDeactivated;
        }

        private void OnActivated(object sender, EventArgs e)
        {
            LoadSymbols();
        }

        private void OnDeactivated(object sender, EventArgs e)
        {
            this.Hide();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _listViewSymbolControl.OnEscape = () => this.Hide();
            _listViewSymbolControl.OnActive = (s) => this.GoToSymbol();
            _listViewSymbolControl.OnLeft = () => _typeSymbolControl.ToLeft();
            _listViewSymbolControl.OnRight = () => _typeSymbolControl.ToRight();

            _typeSymbolControl.ChangedFilter = (symbolFilterModel) => {
                _symbolFilterModel = symbolFilterModel;
                LoadSymbols();
            };
            LoadSymbols();
        }

        private void LoadSymbols()
        {
            _searchId++;
            var currentSearchId = _searchId;
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();

            var symbolFilter = _symbolFilterModel?.SymbolType ?? SymbolTypeFilter.Class;
            var isExternal = _symbolFilterModel?.IsExternal ?? false;
            var cancellationToken = _cancellationTokenSource.Token;
            _listViewSymbolControl.StartAsync(async () => await LoadSymbolsAsync(isExternal, symbolFilter, currentSearchId, cancellationToken));
        }
        
        private async Task LoadSymbolsAsync(bool isExternal, SymbolTypeFilter symbolFilter, ulong currentSearchId, CancellationToken cancellationToken)
        {
            var symbolModels = Array.Empty<SymbolModel>();

            ISearchPattern _searchPattern;
            if (symbolFilter == SymbolTypeFilter.File)
            {
                symbolModels = _dte.SearchFiles();
                _searchPattern = new SearchFilePattern();
            }
            else
            {
                _searchPattern = new SearchSymbolPattern();
                if (symbolFilter == SymbolTypeFilter.Class)
                {
                    symbolModels = await _workspace.SearchTypes(isExternal, cancellationToken);
                }
                else if (symbolFilter == SymbolTypeFilter.Function)
                {
                    symbolModels = await _workspace.SearchMethods(isExternal, cancellationToken);
                }
            }
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            if (currentSearchId == _searchId)
            {
                _listViewSymbolControl.Symbols = symbolModels;
                _listViewSymbolControl.SearchPattern = _searchPattern;
            }
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
                var document = _workspace.CurrentSolution.GetDocument(location.SourceTree);
                await _workspace.TryGoToDefinitionAsync(symbol.Symbol, document.Project, default);
            }
            else
            {
                var project = _workspace.CurrentSolution.Projects.FirstOrDefault();
                await _workspace.TryGoToDefinitionAsync(symbol.Symbol, project, default);
            }

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            this.Hide();
        }
    }
}
