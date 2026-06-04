using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Formatting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GSharper.Assistants
{
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
