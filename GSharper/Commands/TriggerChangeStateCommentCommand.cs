using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using System;
using System.ComponentModel.Design;
using System.Linq;
using System.Threading.Tasks;

namespace GSharper.Commands
{
    /// <summary>
    /// КОманда ля комментирования и разкомментирования текстового фрагмента
    /// </summary>
    public class TriggerChangeStateCommentCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0105;
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
        public static TriggerChangeStateCommentCommand Instance { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyboardShortcutCollectionCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private TriggerChangeStateCommentCommand(AsyncPackage package, OleMenuCommandService commandService)
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
            Instance = new TriggerChangeStateCommentCommand(package, commandService);
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

            ITextBuffer textBuffer = textView.TextBuffer;

            if (!textBuffer.Properties.TryGetProperty(typeof(ITextDocument), out ITextDocument document))
                return;

            ITextSnapshot snapshot = textView.TextSnapshot;
            NormalizedSnapshotSpanCollection selection = textView.Selection.SelectedSpans;

            if (selection.Count == 0) 
                return;

            SnapshotSpan selectedSpan = selection.First();
            ITextSnapshotLine startLine = snapshot.GetLineFromPosition(selectedSpan.Start);
            ITextSnapshotLine endLine = snapshot.GetLineFromPosition(selectedSpan.End);

            int startLineNumber = startLine.LineNumber;
            int endLineNumber = endLine.LineNumber;

            if (endLineNumber > startLineNumber && selectedSpan.End == endLine.Start)
            {
                endLineNumber--;
            }

            var lines = Enumerable
                .Range(startLineNumber, endLineNumber - startLineNumber + 1)
                .Select(snapshot.GetLineFromLineNumber)
                .Where(x => !String.IsNullOrWhiteSpace(x.GetText()))
                .ToArray();

            if (lines.Length < 1)
                return;
            
            var commentPrefix = "//";

            bool allLinesCommented = lines
                .Where(line => !string.IsNullOrWhiteSpace(line.GetText()))
                .All(line => line.GetText().TrimStart().StartsWith(commentPrefix));

            using (ITextEdit edit = snapshot.TextBuffer.CreateEdit())
            {
                if (allLinesCommented)
                {
                    foreach (var line in lines)
                    {
                        string text = line.GetText();
                        int leadingSpacesCount = text.Length - text.TrimStart().Length;
                        string trimmedText = text.TrimStart();

                        int charsToRemove = trimmedText.StartsWith(commentPrefix + " ") ? 3 : 2;

                        int startPosition = line.Start + leadingSpacesCount;
                        edit.Delete(startPosition, charsToRemove);
                    }
                }
                else
                {
                    var leadingSpacesCount = lines
                        .Select(x => x.GetText())
                        .Select(text => text.Length - text.TrimStart().Length)
                        .Min();

                    foreach (var line in lines)
                    {
                        string text = line.GetText();
                        edit.Insert(line.Start + leadingSpacesCount, commentPrefix + " ");
                    }
                }

                edit.Apply();
            }
        }
    }
}
