using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GSharper.Models
{
    public class GitBranchInfo
    {
        public string Name { set; get; }
        public bool IsCurrent { set; get; }
        public bool IsRemote { set; get; }
    }
}
