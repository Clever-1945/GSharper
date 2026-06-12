using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Utilities;
using GSharper.Extensions;
using GSharper.Helpers;
using GSharper.Interfaces;
using GSharper.Models;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace GSharper.Dialogs
{
    /// <summary>
    /// Логика взаимодействия для ShowHistoryFileDialog.xaml
    /// </summary>
    public partial class ShowHistoryFileDialog : Window, IAsyncControl
    {
        private readonly string _fileName;
        private readonly GitHelper _git;
        private IComponentModel _componentModel;
        private IContentType _contentType;

        public ProgressBar ProgressBarFilter => _progressBarFilter;
        public TextBlock TextBlockInfo => _textBlockInfo;

        public int CountFilter { set; get; }

        public ShowHistoryFileDialog(string fileName)
        {
            InitializeComponent();
            _fileName = fileName;
            _git = new GitHelper(fileName);
            _componentModel = (IComponentModel)Package.GetGlobalService(typeof(SComponentModel));

            var contentTypeRegistry = _componentModel.GetService<IContentTypeRegistryService>();
            var fileExtensionRegistry = _componentModel.GetService<IFileExtensionRegistryService>();
            _contentType = fileExtensionRegistry.GetContentTypeForExtension(System.IO.Path.GetExtension(_fileName));
            _contentType = _contentType ?? contentTypeRegistry.GetContentType("TypeScript");

            this.Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            this.StartAsync(async () => await LoadHistoryAsync());
        }

        private async Task AppplyDiffContent(string leftContent, string rightContent)
        {
            await _monacoEditorControl.SetContent(rightContent, leftContent, _contentType.DisplayName.ToLower());
        }

        private async Task LoadHistoryAsync()
        {
            var logs = _git.GetLogs(_fileName);
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            _dataGridCommits.ItemsSource = logs;
            _dataGridCommits.SelectedValue = logs.FirstOrDefault();
        }

        private void OnSelectedCommit(object sender, SelectionChangedEventArgs e)
        {
            var info = ((sender as DataGrid)?.SelectedValue as GitLogInfo);
            if (info != null)
            {
                this.StartAsync(async () => await ShowDiffAsync(info));
            }
        }

        private async Task ShowDiffAsync(GitLogInfo info)
        {
            var current = "";
            var before = "";

            try
            {
                if (info != null)
                    current = _git.GetShowCurrentText(info.Commit, _fileName);
            }
            catch
            {
            }

            try
            {
                if (info != null)
                    before = _git.GetShowBeforeText(info.Commit, _fileName);
            }
            catch
            {
            }

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            await AppplyDiffContent(before, current);
        }
    }
}
