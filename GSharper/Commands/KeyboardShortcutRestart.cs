using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace GSharper.Commands
{
    public class KeyboardShortcutRestart
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0102;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("578213b0-a1b9-49ca-924d-b5488d8e74e4");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        private readonly DTE2 _dte;

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyboardShortcutRestart"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private KeyboardShortcutRestart(AsyncPackage package, OleMenuCommandService commandService)
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
        public static KeyboardShortcutRestart Instance { get; private set; }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new KeyboardShortcutRestart(package, commandService);
        }

        /// <summary>
        /// Shows the tool window when the menu item is clicked.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        private void Execute(object sender, EventArgs e)
        {
            Task.Run(async () =>
            {
                var result = MessageBox.Show("Все шорткаты будут сброшены в дефолтные значения. Согласны?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                {
                    return;
                }

                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                _dte.ExecuteCommand("Tools.CustomizeKeyboard", "/Reset");
            });

            //Properties keyboardProperties = _dte.get_Properties("Environment", "Keyboard");
            //Property schemeProperty = keyboardProperties.Item("SchemeName");
            //schemeProperty.Value = "(Default)";

            // Properties pc = _dte.get_Properties("Environment", "Keyboard");
            //string guidVSStd97String = VSConstants.GUID_VSStandardCommandSet97.ToString("B");
            //_dte.Commands.Raise(guidVSStd97String, (int)VSConstants.VSStd97CmdID.CustomizeKeyboard, null, null);
            // _dte.ExecuteCommand("Tools.CustomizeKeyboard", "/Reset");
        }
    }
}
