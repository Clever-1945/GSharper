using System;
using System.Collections.Generic;
using System.IO;
using GSharper.Assistants;
using GSharper.Extensions;

namespace GSharper.Commands
{
    public class TriggerGoToImplementationsCommand : GSharperCommandBase<TriggerGoToImplementationsCommand>
    {
        private static HashSet<string> _extensionToDefinition = new HashSet<string>(getExtensionToDefinition(), StringComparer.OrdinalIgnoreCase);

        private static IEnumerable<string> getExtensionToDefinition()
        {
            yield return ".xaml";
        }

        public override void Execute(object sender, EventArgs e)
        {
            var symbol = Assistant.GetSymbolUnderCursor();
            if (symbol != null)
            {
                symbol.TryGoToImplementations();
                return;
            }

            var document = Assistant.GetActiveTextView()?.GetDocument();
            if (document != null && document.FilePath != null)
            {
                var extension = Path.GetExtension(document.FilePath);
                if (_extensionToDefinition.Contains(extension))
                {
                    Assistant.TryExecuteCommand("Edit.GoToDefinition");
                }
            }
        }
    }
}
