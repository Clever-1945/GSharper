using GSharper.Assistants;
using GSharper.Extensions;
using System;

namespace GSharper.Commands
{
    public class TriggerGoToBaseTypesCommand : GSharperCommandBase<TriggerGoToImplementationsCommand>
    {
        public override void Execute(object sender, EventArgs e)
        {
            var symbol = Assistant.GetSymbolUnderCursor();
            symbol.TryGoToBaseTypes();
        }
    }
}
