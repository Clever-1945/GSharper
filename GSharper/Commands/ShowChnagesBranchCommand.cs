using System;
using System.IO;
using System.Windows;
using GSharper.Dialogs;
using GSharper.Assistants;

namespace GSharper.Commands
{
    /// <summary>
    /// Команда для отображения изменений между текущий файлом и его версиями в разных бранчал
    /// </summary>
    public class ShowChnagesBranchCommand : GSharperCommandBase<ShowChnagesBranchCommand>
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
                var dialog = new ShowChnagesBranchDialog(activeDocument);
                dialog.Owner = Application.Current.MainWindow;
                dialog.ShowModal();
            }
        }
    }
}
