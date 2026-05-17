using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Formatting;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace GSharper
{
    public static class Assistant
    {
        private static IComponentModel _componentModel;
        private static DTE2 _dte;
        private static VisualStudioWorkspace _workspace;

        private static AssistantTextFormatting _textFormatting;

        public static AssistantTextFormatting TextFormatting => (_textFormatting ?? (_textFormatting = new AssistantTextFormatting()));

        /// <summary>
        /// Получение папки с данными плагина плагина
        /// </summary>
        /// <returns></returns>
        public static DirectoryInfo GetPluginDirectory()
        {
            string path = Environment.ExpandEnvironmentVariables("%appdata%");
            var directory = new DirectoryInfo(path);

            path = Path.Combine(directory.FullName, "Sharper");
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
    }

    public class AssistantTextFormatting
    {
        private IClassificationFormatMapService _formatMapService;
        private IClassificationTypeRegistryService _typeRegistry;
        private IClassificationFormatMap _formatMap;

        private IClassificationType _structType;
        private IClassificationType _classType;
        private IClassificationType _interfaceType;
        private IClassificationType _enumType;
        private IClassificationType _methodType;
        private IClassificationType _keywordType;
        private IClassificationType _identifierType;
        private IClassificationType _delegateType;
        private IClassificationType _parameterType;
        private IClassificationType _typeParameterType;
        private IClassificationType _fieldType;
        private IClassificationType _propertyType;
        private IClassificationType _localType;

        public TextFormattingRunProperties StructProperties { get; }
        public TextFormattingRunProperties ClassProperties { get; }
        public TextFormattingRunProperties InterfaceProperties { get; }
        public TextFormattingRunProperties EnumProperties { get; }
        public TextFormattingRunProperties MethodProperties { get; }
        public TextFormattingRunProperties KeywordProperties { get; }
        public TextFormattingRunProperties IdentifierProperties { get; }
        public TextFormattingRunProperties DelegateProperties { get; }
        public TextFormattingRunProperties ParameterProperties { get; }
        public TextFormattingRunProperties TypeParameterProperties { get; }
        public TextFormattingRunProperties FieldProperties { get; }
        public TextFormattingRunProperties LocalProperties { get; }
        public TextFormattingRunProperties PropertyProperties { get; }

        public AssistantTextFormatting()
        {
            var _componentModel = Assistant.GetComponentModel();
            _formatMapService = _componentModel.GetService<IClassificationFormatMapService>();
            _typeRegistry = _componentModel.GetService<IClassificationTypeRegistryService>();
            _formatMap = _formatMapService.GetClassificationFormatMap("text");

            _structType = _typeRegistry.GetClassificationType("struct name");
            _classType = _typeRegistry.GetClassificationType("class name");
            _interfaceType = _typeRegistry.GetClassificationType("interface name");
            _enumType = _typeRegistry.GetClassificationType("enum name");
            _methodType = _typeRegistry.GetClassificationType("method name");
            _keywordType = _typeRegistry.GetClassificationType("keyword");
            _identifierType = _typeRegistry.GetClassificationType("identifier");
            _delegateType = _typeRegistry.GetClassificationType("delegate name");
            _parameterType = _typeRegistry.GetClassificationType("parameter name");
            _typeParameterType = _typeRegistry.GetClassificationType("type parameter name");
            _fieldType = _typeRegistry.GetClassificationType("field name");
            _propertyType = _typeRegistry.GetClassificationType("property name");
            _localType = _typeRegistry.GetClassificationType("local name");

            StructProperties = _formatMap.GetTextProperties(_structType);
            ClassProperties = _formatMap.GetTextProperties(_classType);
            InterfaceProperties = _formatMap.GetTextProperties(_interfaceType);
            EnumProperties = _formatMap.GetTextProperties(_enumType);
            MethodProperties = _formatMap.GetTextProperties(_methodType);
            KeywordProperties = _formatMap.GetTextProperties(_keywordType);
            IdentifierProperties = _formatMap.GetTextProperties(_identifierType);
            DelegateProperties = _formatMap.GetTextProperties(_delegateType);
            ParameterProperties = _formatMap.GetTextProperties(_parameterType);
            TypeParameterProperties = _formatMap.GetTextProperties(_typeParameterType);
            FieldProperties = _formatMap.GetTextProperties(_fieldType);
            PropertyProperties = _formatMap.GetTextProperties(_propertyType);
            LocalProperties = _formatMap.GetTextProperties(_localType);
        }
    }
}
