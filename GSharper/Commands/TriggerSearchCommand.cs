using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using GSharper.Dialogs;
using System;
using System.Windows;

namespace GSharper.Commands
{
    public class TriggerSearchCommand: GSharperCommandBase<TriggerSearchCommand>
    {
        private SearchDialog _searchDialog;

        /// <summary>
        /// Shows the tool window when the menu item is clicked.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        public override void Execute(object sender, EventArgs e)
        {
            var dialog = _searchDialog ?? (_searchDialog = new SearchDialog());
            dialog.Owner = Application.Current.MainWindow;
            dialog.ShowModal();
        }
    }
}
