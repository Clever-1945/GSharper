using GSharper.Assistants;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

namespace GSharper.Extensions
{
    public static class IWpfTextViewExtensions
    {
        /// <summary>
        /// Получить строку с выделенным текстом
        /// </summary>
        /// <param name="wpfTextView"></param>
        /// <returns></returns>
        public static string GetSelectedText(this IWpfTextView wpfTextView)
        {
            if (wpfTextView == null) 
                return string.Empty;

            if (wpfTextView.Selection != null && !wpfTextView.Selection.IsEmpty)
            {
                return wpfTextView.Selection.StreamSelectionSpan.GetText();
            }
            return string.Empty;
        }

        /// <summary> Получить документ от текстового представления </summary>
        /// <param name="textView"></param>
        /// <returns></returns>
        public static Document GetDocument(this IWpfTextView textView)
        {
            SnapshotPoint caretPoint = textView.Caret.Position.BufferPosition;
            int position = caretPoint.Position;

            var document = caretPoint.Snapshot.GetOpenDocumentInCurrentContextWithChanges();
            return document;
        }
    }
}
