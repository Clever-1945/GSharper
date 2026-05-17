using Microsoft.CodeAnalysis;
using GSharper.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Threading;

namespace GSharper.Extensions
{
    public static class IAsyncControlExtensions
    {
        private static object lock_dispatcher = new object();
        public static void ShowLoading(this IAsyncControl control, bool isLoad)
        {
            control.Dispatcher.Invoke(() =>
            {
                lock (lock_dispatcher)
                {
                    control.CountFilter += (isLoad ? 1 : -1);
                    int countFilter = control.CountFilter;

                    control.TextBlockInfo.Visibility = Visibility.Collapsed;
                    control.ProgressBarFilter.Visibility = countFilter > 0
                        ? Visibility.Visible
                        : Visibility.Collapsed;
                }
            });
        }

        public static void ShowInfo(this IAsyncControl control, string message)
        {
            control.Dispatcher.Invoke(() =>
            {
                lock (lock_dispatcher)
                {
                    control.TextBlockInfo.Text = message ?? "";
                    control.ProgressBarFilter.Visibility = Visibility.Collapsed;
                    control.TextBlockInfo.Visibility = Visibility.Visible;
                }
            });
        }

        public static async Task StartAsync(this IAsyncControl control, Func<Task> action)
        {
            ThreadPool.QueueUserWorkItem((x) => 
            {
                var task = action();
                control.ShowLoading(true);
                task.GetAwaiter().OnCompleted(() =>
                {
                    control.ShowLoading(false);
                    if (task.Status == TaskStatus.Faulted)
                    {
                        control.ShowInfo(task.Exception?.InnerException?.Message ?? task.Exception?.Message);
                    }
                });
            });
        }
    }
}
