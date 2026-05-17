using GSharper.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GSharper.Models
{
    public class SymbolFilterModel
    {
        public bool IsExternal { set; get; }
        public SymbolTypeFilter SymbolType { set; get; }
    }
}
