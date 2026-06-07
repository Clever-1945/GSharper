using EnvDTE;
using EnvDTE80;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;

namespace GSharper.Assistants
{
    public static class Assistant
    {
        private static IComponentModel _componentModel;
        private static DTE2 _dte;
        private static VisualStudioWorkspace _workspace;

        public static Lazy<AssistantTextFormatting> TextFormatting { get; } = new Lazy<AssistantTextFormatting>(() => new AssistantTextFormatting());

        public static Lazy<AssistantDecompile> Decompile { get; } = new Lazy<AssistantDecompile>(() => new AssistantDecompile());

        /// <summary>
        /// Получение папки с данными плагина плагина
        /// </summary>
        /// <returns></returns>
        public static DirectoryInfo GetPluginDirectory()
        {
            string path = Environment.ExpandEnvironmentVariables("%appdata%");
            var directory = new DirectoryInfo(path);

            path = Path.Combine(directory.FullName, "GSharper");
            return Directory.CreateDirectory(path);
        }

        public static DirectoryInfo GetMonacoEditorDirectory()
        {
            var path = Path.Combine(GetPluginDirectory().FullName, "monaco-editor");
            return Directory.CreateDirectory(path);
        }

        public static DirectoryInfo GetScriptsDirectory()
        {
            var path = Path.Combine(GetPluginDirectory().FullName, "scripts");
            return Directory.CreateDirectory(path);
        }

        /// <summary> Извлеч скрипты </summary>
        public static void ExtractScripts()
        {
            var assembly = typeof(Assistant).Assembly;
            string resource = ".Resources.scripts.";
            var scriptsDirectory = GetScriptsDirectory();

            foreach (var resourceName in assembly.GetManifestResourceNames())
            {
                var index = resourceName.IndexOf(resource, StringComparison.OrdinalIgnoreCase);
                if (index >= 0)
                {
                    var scriptFileName = resourceName.Substring(index + resource.Length);
                    var scriptPath = System.IO.Path.Combine(scriptsDirectory.FullName, scriptFileName);

                    using (var stream = assembly.GetManifestResourceStream(resourceName))
                    {
                        using (var fs = File.Create(scriptPath))
                        {
                            stream.CopyTo(fs);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Извлеч файлы Monaco Editor
        /// </summary>
        public static void ExtractMonacoEditor()
        {
            var directory = GetMonacoEditorDirectory();
            if (directory.GetFiles().Length > 0)
                return;

            var assembly = typeof(Assistant).Assembly;
            var resourceName = assembly.GetManifestResourceNames().FirstOrDefault(x => x.EndsWith(".Resources.monaco-editor.zip", StringComparison.OrdinalIgnoreCase));
            if (resourceName == null)
                return;

            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read))
                {
                    archive.ExtractToDirectory(directory.FullName);
                }
            }
        }

        public static TS GetGlobalService<TS, TT>()
        {
            return (TS)Package.GetGlobalService(typeof(TT));
        }

        public static DTE2 GetDte()
        {
            return _dte ?? (_dte = GetGlobalService<DTE2, DTE>());
        }

        public static IComponentModel GetComponentModel()
        {
            return _componentModel ?? (_componentModel = GetGlobalService<IComponentModel, SComponentModel>());
        }

        public static VisualStudioWorkspace GetWorkspace()
        {
            return _workspace ?? (_workspace = GetComponentModel().GetService<VisualStudioWorkspace>());
        }

        public static T GetComponentService<T>() where T : class
        {
            return GetComponentModel().GetService<T>();
        }

        public static IWpfTextView GetActiveTextView()
        {
            var textManager = GetGlobalService<IVsTextManager, SVsTextManager>();
            if (textManager == null)
                return null;

            textManager.GetActiveView(1, null, out IVsTextView textViewCurrent);
            if (textViewCurrent == null)
                return null;

            var componentModel = GetComponentModel();
            var editorAdapterFactory = componentModel.GetService<IVsEditorAdaptersFactoryService>();

            return editorAdapterFactory.GetWpfTextView(textViewCurrent);
        }

        public static ISymbol GetSymbolUnderCursor(IWpfTextView textView = null)
        {
            textView = textView ?? Assistant.GetActiveTextView();
            if (textView == null)
                return null;

            SnapshotPoint caretPoint = textView.Caret.Position.BufferPosition;
            int position = caretPoint.Position;

            var componentModel = GetComponentModel();
            var workspace = componentModel.GetService<VisualStudioWorkspace>();
            if (workspace == null)
                return null;

            var document = caretPoint.Snapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
                return null;

            if (!document.TryGetSemanticModel(out var semanticModel))
                return null;

            if (!document.TryGetSyntaxRoot(out var root))
                return null;

            SyntaxNode node = root.FindToken(position).Parent;
            if (node == null)
                return null;

            SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(node);
            ISymbol symbol = symbolInfo.Symbol ?? symbolInfo.CandidateSymbols.FirstOrDefault() ?? semanticModel.GetDeclaredSymbol(node);
            return symbol;
        }

        public static IVsOutputWindowPane GetOutputPane()
        {
            IVsOutputWindow outputWindow = Package.GetGlobalService(typeof(SVsOutputWindow)) as IVsOutputWindow;
            Guid buildPaneGuid = VSConstants.GUID_BuildOutputWindowPane;
            int hr = outputWindow.GetPane(ref buildPaneGuid, out IVsOutputWindowPane buildPane);

            if (ErrorHandler.Succeeded(hr) && buildPane != null)
            {
                return buildPane;
            }
            return null;
        }

        public static Guid GetMd5(FileInfo file)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = file.OpenRead())
                {
                    byte[] hashBytes = md5.ComputeHash(stream);
                    return new Guid(hashBytes);
                }
            }
        }
    }
}
