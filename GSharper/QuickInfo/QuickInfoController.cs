using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace GSharper.QuickInfo
{
    public class QuickInfoController: IAsyncQuickInfoSource, IIntellisenseController
    {
        private ITextView _textView;
        private EnvDTE80.DTE2 _dte;

        public QuickInfoController(ITextView textView)
        {
            _textView = textView;
            _dte = (Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(EnvDTE.DTE)) as EnvDTE80.DTE2);
        }

        public QuickInfoController()
        {
        }

        public async Task<QuickInfoItem> GetQuickInfoItemAsync(IAsyncQuickInfoSession session, CancellationToken cancellationToken)
        {
            var triggerPoint = session.GetTriggerPoint(session.TextView.TextBuffer.CurrentSnapshot);
            if (!triggerPoint.HasValue) 
                return null;

            Document document = triggerPoint.Value.Snapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null) 
                return null;

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);

            int position = triggerPoint.Value.Position;
            ISymbol symbol = await SymbolFinder.FindSymbolAtPositionAsync(document, position, cancellationToken);

            if (symbol != null)
            {
                var symbolName = symbol.Name;
                var kind = symbol.Kind;
            }

            ITrackingSpan trackingSpan = null;
            var root = await document.GetSyntaxRootAsync(cancellationToken);
            SyntaxNode node = null;
            if (root != null)
            {
                var token = root.FindToken(position);
                if (token != null)
                {
                    trackingSpan = triggerPoint.Value.Snapshot.CreateTrackingSpan(token.Span.Start, token.Span.Length, SpanTrackingMode.EdgeInclusive);
                    node = token.Parent;
                }
            }

            return new QuickInfoItem(trackingSpan, new QuickInfoBlock(session, symbol, node));
        }

        public void Dispose()
        {
            
        }

        public void ConnectSubjectBuffer(ITextBuffer subjectBuffer) 
        {
        }
        public void DisconnectSubjectBuffer(ITextBuffer subjectBuffer) 
        {
        }
        public void Detach(ITextView textView) 
        {
        }


        public void OnTextViewMouseHover(object sender, MouseHoverEventArgs e)
        {
            // Находим активные сессии QuickInfo


            // Здесь вы можете запустить СВОЮ сессию, 
            // которая будет использовать только ваш Source
        }
    }
}
