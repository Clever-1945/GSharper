using GSharper.Assistants;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using System;
using System.Linq;

namespace GSharper.Commands
{
    /// <summary> Команда изменения регистра выделенного слова в документе </summary>
    public class TriggerChangeCaseCommand : GSharperCommandBase<TriggerChangeCaseCommand>
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

            ITextSnapshot snapshot = textView.TextSnapshot;

            if (!textView.Selection.IsEmpty)
            {
                SnapshotSpan targetSpan = textView.Selection.SelectedSpans.First();

                string currentText = targetSpan.GetText();
                if (String.IsNullOrWhiteSpace(currentText)) 
                    return;

                bool isAllUpper = currentText == currentText.ToUpper();

                string transformedText = isAllUpper
                    ? currentText.ToLower()
                    : currentText.ToUpper();

                using (ITextEdit edit = snapshot.TextBuffer.CreateEdit())
                {
                    edit.Replace(targetSpan, transformedText);
                    edit.Apply();
                }

                textView.Selection.Select(new SnapshotSpan(textView.TextSnapshot, targetSpan.Start, targetSpan.Length), false);
            }
        }
    }
}