using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;

namespace GSharper.Extensions
{
    public static class InlineExtensions
    {
        public static Inline CreateLink(this Inline inline, ISymbol symbol, bool create = false)
        {
            if (!create)
                return inline;

            return new Hyperlink(inline)
            {
                TextDecorations = null,
                Tag = symbol
            };
        }
    }
}
