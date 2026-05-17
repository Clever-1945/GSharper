using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GSharper.Models
{
    public class SymbolProjectDefinition
    {
        public ISymbol Symbol { get; }
        public Project Project { get; }
        public MetadataReference Reference { get; }

        public SymbolProjectDefinition(ISymbol symbol, Project project, MetadataReference reference)
        {
            Symbol = symbol;
            Project = project;
            Reference = reference;
        }
    }
}
