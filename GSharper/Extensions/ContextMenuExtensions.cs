using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace GSharper.Extensions
{
    public static class ContextMenuExtensions
    {
        public static void Add(this ContextMenu contextMenu, string header, object tag, Action<object, RoutedEventArgs> action)
        {
            MenuItem item = new MenuItem();
            item.Header = header;
            item.Tag = tag;
            item.Click += (object sender, RoutedEventArgs e) => action(sender, e);
            contextMenu.Items.Add(item);
        }
    }
}
