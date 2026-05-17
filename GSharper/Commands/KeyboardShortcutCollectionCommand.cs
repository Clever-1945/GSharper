using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using GSharper.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows;

namespace GSharper.Commands
{
    public class KeyboardShortcutCollectionCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0101;

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
        /// Initializes a new instance of the <see cref="KeyboardShortcutCollectionCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private KeyboardShortcutCollectionCommand(AsyncPackage package, OleMenuCommandService commandService)
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
        public static KeyboardShortcutCollectionCommand Instance { get; private set; }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider => this.package;

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new KeyboardShortcutCollectionCommand(package, commandService);
        }

        private Dictionary<string, string> GetShortcuts()
        {
            var shortcut = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            shortcut["View.NavigateForward"] = "Ctrl+Alt+Стрелка вправо";
            shortcut["View.NavigateBackward"] = "Ctrl+Alt+Стрелка влево";
            shortcut["Debug.QuickWatch"] = "Shift+F9";
            shortcut["Edit.QuickInfo"] = "Ctrl+Q";
            shortcut["Edit.FormatSelection"] = "Ctrl+Alt+L";
            shortcut["Edit.GoToImplementation"] = "Ctrl+Alt+Shift+B";
            //

            shortcut["Sharper.triggerSearchDialog"] = "Ctrl+N";
            
            // Edit.FormatSelection
            return shortcut;
        }

        private void DeleteShortcuts(string[] shortcuts, EnvDTE.Commands commands)
        {
            foreach (Command cmd in commands)
            {
                if (String.IsNullOrWhiteSpace(cmd.Name) || String.IsNullOrWhiteSpace(cmd.LocalizedName))
                {
                    continue;
                }
                object[] objectShortcuts = (cmd.Bindings as object[]) ?? Array.Empty<object>();
                if (objectShortcuts.Length > 0)
                {
                    string[] textShortcuts = objectShortcuts.Select(x => x as string).Where(x => !String.IsNullOrWhiteSpace(x)).ToArray();
                    if (textShortcuts.Length > 0)
                    {
                        foreach(var shortcut in shortcuts)
                        {
                            if (textShortcuts.First().Contains($"::{shortcut}", StringComparison.OrdinalIgnoreCase))
                            {
                                cmd.Bindings = new object[0];
                                break;
                            }
                            else if (textShortcuts.Skip(1).Any(x => String.Equals(x, shortcut, StringComparison.OrdinalIgnoreCase)))
                            {
                                cmd.Bindings = new object[0];
                                break;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Shows the tool window when the menu item is clicked.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        private void Execute(object sender, EventArgs e)
        {
            var commands = _dte.Commands;
            var report = new List<string>();

            var shortcuts = GetShortcuts();
            System.Windows.MessageBox.Show("Перед примененией комбинаций клавиш обязательно переключите раскладку на английскую!");

            DeleteShortcuts(shortcuts.Select(x => x.Value).ToArray(), commands);

            foreach (Command cmd in commands)
            {
                if (String.IsNullOrWhiteSpace(cmd.Name) || String.IsNullOrWhiteSpace(cmd.LocalizedName))
                {
                    continue;
                }
                var shortcut = shortcuts.GetValueOrDefault(cmd.LocalizedName) ?? shortcuts.GetValueOrDefault(cmd.Name);
                if (shortcut != null)
                {
                    shortcut = shortcut.Replace("Стрелка вправо", "Right Arrow");
                    shortcut = shortcut.Replace("Стрелка влево", "Left Arrow");

                    cmd.Bindings = new object[] { $"Везде::{shortcut}" };
                }
            }
        }
    }
}
