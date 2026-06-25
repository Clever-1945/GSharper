using GSharper.Assistants;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using System;
using System.IO;
using System.Linq;

namespace GSharper.Commands
{
    /// <summary>
    /// Команда ля комментирования и разкомментирования текстового фрагмента
    /// </summary>
    public class TriggerChangeStateCommentCommand : GSharperCommandBase<TriggerChangeStateCommentCommand>
    {
        public class CommentSymbols
        {
            public string Prefix;
            public string Postfix;
        }

        private CommentSymbols GetCommentSymbols(ITextDocument document)
        {
            var extension = Path.GetExtension(document?.FilePath ?? "").ToLower();
            switch(extension)
            {
                case ".xml":
                case ".xaml":
                case ".html":
                    return new CommentSymbols()
                    {
                        Prefix = "<!--",
                        Postfix = "-->"
                    };
                case ".cshtml":
                    return new CommentSymbols()
                    {
                        Prefix = "@*",
                        Postfix = "*@"
                    };
                case ".sql":
                    return new CommentSymbols()
                    {
                        Prefix = "--",
                    };
            }

            return new CommentSymbols()
            {
                Prefix = "//"
            };
        }

        /// <summary>
        /// Shows the tool window when the menu item is clicked.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        public override void Execute(object sender, EventArgs e)
        {
            IWpfTextView textView = Assistant.GetActiveTextView();
            if (textView == null) 
                return;

            ITextBuffer textBuffer = textView.TextBuffer;

            if (!textBuffer.Properties.TryGetProperty(typeof(ITextDocument), out ITextDocument document))
                return;

            ITextSnapshot snapshot = textView.TextSnapshot;
            NormalizedSnapshotSpanCollection selection = textView.Selection.SelectedSpans;

            if (selection.Count < 1) 
                return;

            SnapshotSpan selectedSpan = selection.First();
            ITextSnapshotLine startLine = snapshot.GetLineFromPosition(selectedSpan.Start);
            ITextSnapshotLine endLine = snapshot.GetLineFromPosition(selectedSpan.End);

            int startLineNumber = startLine.LineNumber;
            int endLineNumber = endLine.LineNumber;

            if (endLineNumber > startLineNumber && selectedSpan.End == endLine.Start)
            {
                endLineNumber--;
            }

            var lines = Enumerable
                .Range(startLineNumber, endLineNumber - startLineNumber + 1)
                .Select(snapshot.GetLineFromLineNumber)
                .Where(x => !String.IsNullOrWhiteSpace(x.GetText()))
                .ToArray();

            if (lines.Length < 1)
                return;

            var commentSymbols = GetCommentSymbols(document);
            bool allLinesCommented = lines
                 .Where(line => !string.IsNullOrWhiteSpace(line.GetText()))
                 .All(line => 
                 {
                     var isPrefix = !String.IsNullOrWhiteSpace(commentSymbols.Prefix);
                     var isCpmmentPrefix = !isPrefix || (isPrefix && line.GetText().TrimStart().StartsWith(commentSymbols.Prefix));

                     var isPostfix = !String.IsNullOrWhiteSpace(commentSymbols.Postfix);
                     var isCpmmentPostfix = !isPostfix || (isPostfix && line.GetText().TrimEnd().EndsWith(commentSymbols.Postfix));

                     return isCpmmentPrefix && isCpmmentPostfix;
                 });

            if (allLinesCommented)
                Assistant.TryExecuteCommand("Edit.UncommentSelection");
            else
                Assistant.TryExecuteCommand("Edit.CommentSelection");
        }
    }
}
