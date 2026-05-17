using EnvDTE;
using EnvDTE80;
using Microsoft.CodeAnalysis;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Differencing;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using GSharper.Controls;
using GSharper.Extensions;
using GSharper.Helpers;
using GSharper.Interfaces;
using GSharper.Models;
using System;
using System.Collections.Generic;
using System.IO;
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
using System.Windows.Shapes;

namespace GSharper.Dialogs
{
    /// <summary>
    /// Логика взаимодействия для ShowHistoryFileDialog.xaml
    /// </summary>
    public partial class ShowHistoryFileDialog : DialogWindow, IAsyncControl
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
            this.Width = (this.Owner.Width / 100) * 80;
            this.Height = (this.Owner.Height / 100) * 80;

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
