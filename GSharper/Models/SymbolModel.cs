using EnvDTE;
using Microsoft.CodeAnalysis;
using GSharper.Enums;
using GSharper.Extensions;
using System;
using System.IO;
using System.Linq;

namespace GSharper.Models
{
    public class SymbolModel
    {
        public Tuple<FileInfo> _projectFile;
        public ProjectItem ProjectItem { get; }
        public ISymbol Symbol { get; }
        public INamedTypeSymbol TypeSymbol { get; }
        public IMethodSymbol MethodSymbol { get; }
        /// <summary> Вес элемента при фильтрации </summary>
        public int Weight { set; get; }

        public SymbolModel(ProjectItem projectItem, int weight = 0)
        {
            ProjectItem = projectItem;
            Weight = weight;
        }

        public SymbolModel(ISymbol symbol, int weight = 0)
        {
            Symbol = symbol;
            TypeSymbol = symbol as INamedTypeSymbol;
            MethodSymbol = symbol as IMethodSymbol;
            Weight = weight;
        }

        public FileInfo GetProjectFile()
        {
            if (_projectFile == null)
            {
                _projectFile = new Tuple<FileInfo>(ProjectItem?.GetFiles().FirstOrDefault());
            }
            return _projectFile.Item1;
        }
    }
}
