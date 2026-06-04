using EnvDTE;
using EnvDTE80;
using GSharper.Assistants;
using GSharper.Dialogs;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace GSharper.Commands
{
    public class TriggerDecompilationPackagesCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0108;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("578213b0-a1b9-49ca-924d-b5488d8e74e4");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        /// <summary>
        /// Initializes a new instance of the <see cref="TriggerSearchCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private TriggerDecompilationPackagesCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static TriggerDecompilationPackagesCommand Instance { get; private set; }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new TriggerDecompilationPackagesCommand(package, commandService);
        }

        /// <summary>
        /// Shows the tool window when the menu item is clicked.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        private void Execute(object sender, EventArgs e)
        {
            var dialog = new SelectAssemblyDialog();
            dialog.Owner = Application.Current.MainWindow;
            dialog.ShowModal();
            if (!dialog.IsOk)
                return;

            var listSelectedProjectPackage = dialog.ListSelectedProjectPackage;
            if (listSelectedProjectPackage.Length < 1)
                return;

            ThreadPool.QueueUserWorkItem(s =>
            {
                foreach (var assemblyFile in listSelectedProjectPackage)
                {
                    var assemblyInfo = Assistant.Decompile.Value.GetDecompiledInfo(assemblyFile.Dll.FullName);
                    if (assemblyInfo == null)
                    {
                        Assistant.Decompile.Value.DecompileAssembly(assemblyFile.Dll.FullName);
                    }
                    
                    assemblyInfo = Assistant.Decompile.Value.GetDecompiledInfo(assemblyFile.Dll.FullName);
                    if (assemblyInfo == null)
                        continue;

                    var pdbFile = Path.ChangeExtension(assemblyFile.Dll.FullName, ".pdb");
                    var dllFile = assemblyFile.Dll.FullName;
                    if (File.Exists(pdbFile))
                        File.Delete(pdbFile);

                    if (File.Exists(dllFile))
                        File.Delete(dllFile);

                    File.Copy(assemblyInfo.Pdb, pdbFile);
                    File.Copy(assemblyInfo.Dll, dllFile);
                }
            });
        }
    }
}
