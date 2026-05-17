using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace GSharper.Extensions
{
    public static class DependencyObjectExtensions
    {
        public static List<T> FindVisualChildren<T>(this DependencyObject depObj) where T : DependencyObject
        {
            List<T> children = new List<T>();
            if (depObj == null) return children;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                if (child != null && child is T)
                {
                    children.Add((T)child);
                }

                children.AddRange(FindVisualChildren<T>(child));
            }
            return children;
        }
    }
}
