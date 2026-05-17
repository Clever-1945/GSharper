using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Utilities;
using GSharper.Extensions;
using GSharper.Helpers;
using GSharper.Interfaces;
using GSharper.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace GSharper.Dialogs
{
    /// <summary>
    /// Логика взаимодействия для ShowChnagesBranchDialog.xaml
    /// </summary>
    public partial class ShowChnagesBranchDialog : DialogWindow, IAsyncControl
    {
        private readonly string _contentDocument;
        private readonly EnvDTE.Document _activeDocument;
        private readonly GitHelper _git;
        private IComponentModel _componentModel;
        private IContentType _contentType;
        private GitBranchInfo[] _listBranch;

        public ProgressBar ProgressBarFilter => _progressBarFilter;
        public TextBlock TextBlockInfo => _textBlockInfo;

        public int CountFilter { set; get; }

        public ShowChnagesBranchDialog(EnvDTE.Document activeDocument)
        {
            InitializeComponent();

            _activeDocument = activeDocument;
            EnvDTE.TextDocument textDoc = (EnvDTE.TextDocument)activeDocument.Object("TextDocument");

            // Создаем точку в начале и в конце документа
            EnvDTE.EditPoint startPoint = textDoc.StartPoint.CreateEditPoint();
            EnvDTE.EditPoint endPoint = textDoc.EndPoint.CreateEditPoint();
            _contentDocument = startPoint.GetText(endPoint);

            _git = new GitHelper(activeDocument.FullName);
            _componentModel = (IComponentModel)Package.GetGlobalService(typeof(SComponentModel));

            var contentTypeRegistry = _componentModel.GetService<IContentTypeRegistryService>();
            var fileExtensionRegistry = _componentModel.GetService<IFileExtensionRegistryService>();
            _contentType = fileExtensionRegistry.GetContentTypeForExtension(System.IO.Path.GetExtension(activeDocument.FullName));
            _contentType = _contentType ?? contentTypeRegistry.GetContentType("TypeScript");

            _listBranchControl.OnSelectedBranch = (branch) =>
            {
                this.StartAsync(async () => await OnSelectedBranch(branch));
            };
            this.Loaded += OnLoaded;
        }

        private async Task OnSelectedBranch(GitBranchInfo branch)
        {
            var contentBranch = _git.GetContentFile(branch.Name);
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            await _monacoEditorControl.SetContent(_contentDocument, contentBranch, _contentType.DisplayName.ToLower(), false);
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            this.Width = (this.Owner.Width / 100) * 80;
            this.Height = (this.Owner.Height / 100) * 80;

            _monacoEditorControl.OnSave = (text) =>
            {
                EnvDTE.TextDocument textDoc = (EnvDTE.TextDocument)_activeDocument.Object("TextDocument");

                EnvDTE.EditPoint startPoint = textDoc.StartPoint.CreateEditPoint();
                EnvDTE.EditPoint endPoint = textDoc.EndPoint.CreateEditPoint();

                startPoint.ReplaceText(endPoint, text, (int)EnvDTE.vsEPReplaceTextOptions.vsEPReplaceTextKeepMarkers);
            };

            this.StartAsync(async () => await LoadListBranchAsync());
        }

        private async Task LoadListBranchAsync()
        {
            _listBranch = _git.GetListBranch();
            Dispatcher.Invoke(() =>
            {
                _listBranchControl.SetListBranch(_listBranch);
                _monacoEditorControl.SetContent(string.Empty, string.Empty, _contentType.DisplayName, true);
            });
        }
    }
}
