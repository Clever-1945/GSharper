using GSharper.Assistants;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using System;
using System.ComponentModel.Design;
using System.Linq;
using System.Threading.Tasks;

namespace GSharper.Commands
{
    /// <summary>
    /// Команда ля комментирования и разкомментирования текстового фрагмента
    /// </summary>
    public class TriggerChangeStateCommentCommand : GSharperCommandBase<TriggerChangeStateCommentCommand>
    {
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
            
            var commentPrefix = "//";

            bool allLinesCommented = lines
                .Where(line => !string.IsNullOrWhiteSpace(line.GetText()))
                .All(line => line.GetText().TrimStart().StartsWith(commentPrefix));

            using (ITextEdit edit = snapshot.TextBuffer.CreateEdit())
            {
                if (allLinesCommented)
                {
                    foreach (var line in lines)
                    {
                        string text = line.GetText();
                        int leadingSpacesCount = text.Length - text.TrimStart().Length;
                        string trimmedText = text.TrimStart();

                        int charsToRemove = trimmedText.StartsWith(commentPrefix + " ") ? 3 : 2;

                        int startPosition = line.Start + leadingSpacesCount;
                        edit.Delete(startPosition, charsToRemove);
                    }
                }
                else
                {
                    var leadingSpacesCount = lines
                        .Select(x => x.GetText())
                        .Select(text => text.Length - text.TrimStart().Length)
                        .Min();

                    foreach (var line in lines)
                    {
                        string text = line.GetText();
                        edit.Insert(line.Start + leadingSpacesCount, commentPrefix + " ");
                    }
                }

                edit.Apply();
            }
        }
    }
}
