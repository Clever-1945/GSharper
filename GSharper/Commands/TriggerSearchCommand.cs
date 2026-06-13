using System;
using GSharper.Dialogs;
using GSharper.Extensions;

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
            dialog.ShowInCenter();
        }
    }
}
