using GSharper.Assistants;
using GSharper.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GSharper.Commands
{
    public class TriggerGoToImplementationsCommand : GSharperCommandBase<TriggerGoToImplementationsCommand>
    {
        public override void Execute(object sender, EventArgs e)
        {
            var symbol = Assistant.GetSymbolUnderCursor();
            symbol.TryGoToImplementations();
        }
    }
}
