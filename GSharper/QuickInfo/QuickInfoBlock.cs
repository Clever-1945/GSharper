using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Language.Intellisense;
using GSharper.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace GSharper.QuickInfo
{
    public class QuickInfoBlock
    {
        private ISymbol _symbol;
        private SyntaxNode _node;
        private IAsyncQuickInfoSession _session;

        public QuickInfoBlock(IAsyncQuickInfoSession session, ISymbol symbol, SyntaxNode node)
        {
            _symbol = symbol;
            _node = node;
            _session = session;
        }

        public UIElement ToUI()
        {
            return _symbol == null ? null : new QuickInfoBlockControl(_session, _symbol, _node);
        }
    }
}
