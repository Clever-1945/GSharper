using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using System;
using System.ComponentModel.Design;
using System.Linq;
using System.Threading.Tasks;

namespace GSharper.Commands
{
    /// <summary> Команда изменения регистра выделенного слова в документе </summary>
    public class TriggerChangeCaseCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0106;
        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("578213b0-a1b9-49ca-924d-b5488d8e74e4");
        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static TriggerChangeCaseCommand Instance { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyboardShortcutCollectionCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private TriggerChangeCaseCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new TriggerChangeCaseCommand(package, commandService);
        }

        /// <summary>
        /// Shows the tool window when the menu item is clicked.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        private void Execute(object sender, EventArgs e)
        {
            IWpfTextView textView = Assistant.GetActiveTextView();
            if (textView == null)
                return;

            ITextSnapshot snapshot = textView.TextSnapshot;

            if (!textView.Selection.IsEmpty)
            {
                SnapshotSpan targetSpan = textView.Selection.SelectedSpans.First();

                string currentText = targetSpan.GetText();
                if (String.IsNullOrWhiteSpace(currentText)) 
                    return;

                bool isAllUpper = currentText == currentText.ToUpper();

                string transformedText = isAllUpper
                    ? currentText.ToLower()
                    : currentText.ToUpper();

                using (ITextEdit edit = snapshot.TextBuffer.CreateEdit())
                {
                    edit.Replace(targetSpan, transformedText);
                    edit.Apply();
                }

                textView.Selection.Select(new SnapshotSpan(textView.TextSnapshot, targetSpan.Start, targetSpan.Length), false);
            }
        }
    }
}