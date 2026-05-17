using Microsoft.VisualStudio.Shell;
using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Wpf;
using Newtonsoft.Json;
using GSharper.Extensions;
using GSharper.Helpers;
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
    /// Логика взаимодействия для MonacoEditorControl.xaml
    /// </summary>
    public partial class MonacoEditorControl : UserControl
    {
        private Task<WebViewScope> _initTask;
        private WebViewScope _viewScope;
        private WebViewMonacoStore _monacoStore = new WebViewMonacoStore();
        public Action<string> OnSave;

        public MonacoEditorControl()
        {
            InitializeComponent();
            _viewScope = _monacoStore.GetView();
            _gridMonaco.Children.Add(_viewScope.View);
            _initTask = _viewScope.InitTask;
            _viewScope.OnSave = () => OnSavePrivate();
            this.Unloaded += MonacoEditorControl_Unloaded;
        }

        private async Task OnSavePrivate()
        {
            var text = await _viewScope.GetModifiedText();
            OnSave?.Invoke(text);
        }

        private void MonacoEditorControl_Unloaded(object sender, RoutedEventArgs e)
        {
            _gridMonaco.Children.Clear();
            _monacoStore.ReleaseView(_viewScope.View);
        }

        public async Task SetContent(string currentContent, string beforeContent, string language, bool readOnly = true)
        {
            var web = await _initTask;
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var currentContentJson = JsonConvert.SerializeObject(currentContent);
            var beforeContentJson = JsonConvert.SerializeObject(beforeContent);

            var script = $"assistant.setModel({beforeContentJson}, {currentContentJson}, '{language}', {(readOnly ? "1" : "0")})";
            await _viewScope.View.ExecuteScriptAsync(script);
        }
    }
}
