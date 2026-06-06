using EnvDTE;
using EnvDTE80;
using GSharper.Dialogs;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO.Packaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GSharper.Commands
{
    public class TriggerQuickInfoDialogCommand : GSharperCommandBase<TriggerQuickInfoDialogCommand>
    {
        /// <summary>
        /// Shows the tool window when the menu item is clicked.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        public override void Execute(object sender, EventArgs e)
        {
            ToolWindowPane window = this.package.FindToolWindow(
                typeof(QuickInfoBlockDialog),
                0,
                create: true);

            // 3. Проверяем, что окно создано, и запрашиваем его фрейм (оболочку)
            if (window == null || window.Frame == null)
            {
                throw new NotSupportedException("Не удалось создать Tool Window");
            }

            // 4. Показываем окно на экране
            var windowFrame = (IVsWindowFrame)window.Frame;
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }
    }
}
