using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Threading;

namespace GSharper.Interfaces
{
    public interface IAsyncControl
    {
        Dispatcher Dispatcher { get; }
        ProgressBar ProgressBarFilter { get; }
        TextBlock TextBlockInfo { get; }
        int CountFilter { set; get; }
    }
}
