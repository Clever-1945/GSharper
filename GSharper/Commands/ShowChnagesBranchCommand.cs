using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using GSharper.Dialogs;
using System;
using System.ComponentModel.Design;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace GSharper.Commands
{
    /// <summary>
    /// Команда для отображения изменений между текущий файлом и его версиями в разных бранчал
    /// </summary>
    public class ShowChnagesBranchCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0102;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("545A271A-937C-4DAA-951F-CF155A82C3FA");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        private readonly DTE2 _dte;

        public ShowChnagesBranchCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);

            _dte = (Package.GetGlobalService(typeof(DTE)) as DTE2);
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static ShowChnagesBranchCommand Instance { get; private set; }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new ShowChnagesBranchCommand(package, commandService);
        }

        /// <summary>
        /// Shows the tool window when the menu item is clicked.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        private void Execute(object sender, EventArgs e)
        {
            EnvDTE.Document activeDocument = _dte.ActiveDocument;
            if (activeDocument != null && File.Exists(activeDocument.FullName))
            {
                var dialog = new ShowChnagesBranchDialog(activeDocument);
                dialog.Owner = Application.Current.MainWindow;
                dialog.ShowModal();
            }
        }
    }
}
