
using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Design;
using System.Threading.Tasks;

namespace GSharper.Commands
{
    public class GSharperCommandBase<T> where T: GSharperCommandBase<T>
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public int CommandId;
        // public int CommandId = 0x0103;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static Guid CommandSet = new Guid("578213b0-a1b9-49ca-924d-b5488d8e74e4");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        public AsyncPackage package;

        public static async Task<T> InitializeAsync(AsyncPackage package, int commandId, Guid? commandSet = null)
        {
            if (commandSet != null)
                CommandSet = commandSet.Value;

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;

            var instace = Activator.CreateInstance(typeof(T)) as T;
            instace.package = package ?? throw new ArgumentNullException(nameof(package));
            instace.CommandId = commandId;

            var menuCommandID = new CommandID(CommandSet, instace.CommandId);
            var menuItem = new MenuCommand(instace.Execute, menuCommandID);
            commandService.AddCommand(menuItem);

            return instace;
        }

        /// <summary>
        /// Shows the tool window when the menu item is clicked.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        public virtual void Execute(object sender, EventArgs e)
        {

        }
    }
}
