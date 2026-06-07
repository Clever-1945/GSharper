using Microsoft.CodeAnalysis;

namespace GSharper.Models
{
    public class SymbolProjectDefinition
    {
        public ISymbol Symbol { get; }
        public Project Project { get; }
        public MetadataReference Reference { get; }
        public Compilation Compilation { get; }

        public SymbolProjectDefinition(ISymbol symbol, Project project, MetadataReference reference, Compilation compilation)
        {
            Symbol = symbol;
            Project = project;
            Reference = reference;
            Compilation = compilation;
        }
    }
}
