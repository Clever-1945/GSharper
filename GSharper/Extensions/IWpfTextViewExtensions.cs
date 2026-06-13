using Microsoft.VisualStudio.Text.Editor;

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
    }
}
