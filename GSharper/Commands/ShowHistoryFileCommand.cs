using GSharper.Dialogs;
using System;
using System.IO;
using System.Windows;
using GSharper.Assistants;
using GSharper.Extensions;

namespace GSharper.Commands
{
    public class ShowHistoryFileCommand : GSharperCommandBase<ShowHistoryFileCommand>
    {
        /// <summary>
        /// Shows the tool window when the menu item is clicked.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        public override void Execute(object sender, EventArgs e)
        {
            EnvDTE.Document activeDocument = Assistant.GetDte().ActiveDocument;
            if (activeDocument != null && File.Exists(activeDocument.FullName))
            {
                var dialog = new ShowHistoryFileDialog(activeDocument.FullName);
                dialog.ShowInCenter(80);
            }
        }
    }
}
