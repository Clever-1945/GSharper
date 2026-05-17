using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Wpf;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Instrumentation;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Shapes;

namespace GSharper.Extensions
{
    public class WebViewScope
    {
        public class JsEventData
        {
            public string action { set; get; }
        }

        public Microsoft.Web.WebView2.Wpf.WebView2 View { get; }

        public Task<WebViewScope> InitTask { get; }

        public Action OnSave { set; get; }

        public WebViewScope(Microsoft.Web.WebView2.Wpf.WebView2 view)
        { 
            this.View = view;

            var taskCompletion = new TaskCompletionSource<WebViewScope>();
            this.Init(() =>
            {
                taskCompletion.SetResult(this);
            });
            this.InitTask = taskCompletion.Task;
        }

        public async Task Init(Action onCompletion)
        {
            View.Loaded += async (object senderL, System.Windows.RoutedEventArgs el) => await OnLoaded(onCompletion);
        }

        public async Task<string> GetModifiedText()
        {
            await InitTask;
            var result = await View.ExecuteScriptAsync("assistant.getModifiedText()");
            var text = JsonConvert.DeserializeObject<string>(result);
            return text;
        }

        private async Task OnLoaded(Action onCompletion)
        {
            View.CoreWebView2InitializationCompleted += (object? senderI, CoreWebView2InitializationCompletedEventArgs e) =>
            {
                Assistant.ExtractMonacoEditor();
                Assistant.ExtractScripts();

                string monacoEditorScriptPath = Assistant.GetMonacoEditorDirectory().FullName;
                string scriptsPath = Assistant.GetScriptsDirectory().FullName;
                View.CoreWebView2.SetVirtualHostNameToFolderMapping("monaco-editor-scripts", monacoEditorScriptPath, CoreWebView2HostResourceAccessKind.Allow);
                View.CoreWebView2.SetVirtualHostNameToFolderMapping("app.local", scriptsPath, CoreWebView2HostResourceAccessKind.Allow);

                View.CoreWebView2.WebMessageReceived += (object? senderR, CoreWebView2WebMessageReceivedEventArgs er) =>
                {
                    var ev = JsonConvert.DeserializeObject<JsEventData>(er.WebMessageAsJson);
                    if (String.Equals(ev?.action, "init-monaco-editor"))
                    {
                        onCompletion();
                    }
                    else if (String.Equals(ev?.action, "save-content"))
                    {
                        OnSave?.Invoke();
                    }
                };
                View.Source = new Uri($"https://app.local/index.html");
            };

            if (View.CoreWebView2 == null)
            {
                string userDataFolder = System.IO.Path.Combine(Assistant.GetScriptsDirectory().Parent.FullName, "browser_data");
                var env = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
                await View.EnsureCoreWebView2Async(env);
            }
        }
    }

    public static class WebView2Extensions
    {
        public static WebViewScope CreateScope(this Microsoft.Web.WebView2.Wpf.WebView2 instance)
        {
            return new WebViewScope(instance);
        }
    }
}
