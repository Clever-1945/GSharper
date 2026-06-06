using System;
using System.Windows;
using GSharper.Assistants;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;

namespace GSharper.Commands
{
    public class KeyboardShortcutRestart : GSharperCommandBase<KeyboardShortcutRestart>
    {
        /// <summary>
        /// Shows the tool window when the menu item is clicked.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        public override void Execute(object sender, EventArgs e)
        {
            Task.Run(async () =>
            {
                var result = MessageBox.Show("Все шорткаты будут сброшены в дефолтные значения. Согласны?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                {
                    return;
                }

                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                Assistant.GetDte().ExecuteCommand("Tools.CustomizeKeyboard", "/Reset");
            });
        }
    }
}
