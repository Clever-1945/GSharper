using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GSharper.Models
{
    public class HyperlinkTagGoToSymbols
    {
        public ISymbol[] Symbols { get; }

        public HyperlinkTagGoToSymbols(ISymbol[] symbols)
        {
            Symbols = symbols ?? Array.Empty<ISymbol>();
        }
    }
}
