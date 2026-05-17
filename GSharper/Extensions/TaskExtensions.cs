using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace GSharper.Extensions
{
    public static class TaskExtensions
    {
        public static Task<RT> OnCompleted<RT>(this Task<RT> task, Func<Task<RT>, bool> filter, Action<Task<RT>> action)
        {
            task.GetAwaiter().OnCompleted(() =>
            {
                if (filter(task))
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        action?.Invoke(task);
                    });
                }
            });
            return task;
        }

        public static Task OnCompleted(this Task task, Func<Task, bool> filter, Action<Task> action)
        {
            task.GetAwaiter().OnCompleted(() =>
            {
                if (filter(task))
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        action?.Invoke(task);
                    });
                }
            });
            return task;
        }

        public static Task<RT> Then<RT>(this Task<RT> task, Action<RT> action)
        {
            return task.OnCompleted((t) => t.Status == TaskStatus.RanToCompletion, (t) =>
            {
                action?.Invoke(task.Result);
            });
        }

        public static Task Then(this Task task, Action action)
        {
            return task.OnCompleted((t) => t.Status == TaskStatus.RanToCompletion, (t) =>
            {
                action?.Invoke();
            });
        }

        public static Task<RT> Catch<RT>(this Task<RT> task, Action<Exception> action)
        {
            return task.OnCompleted((t) => t.Status == TaskStatus.Faulted, (t) =>
            {
                action?.Invoke(t.Exception?.InnerException);
            });
        }

        public static Task Catch(this Task task, Action<Exception> action)
        {
            return task.OnCompleted((t) => t.Status == TaskStatus.Faulted, (t) =>
            {
                action?.Invoke(t.Exception?.InnerException);
            });
        }

        public static Task<RT> Done<RT>(this Task<RT> task, Action<TaskDoneInfo<RT>> action)
        {
            return task.OnCompleted((t) => t.Status == TaskStatus.Faulted || t.Status == TaskStatus.RanToCompletion, (t) =>
            {
                action?.Invoke(new TaskDoneInfo<RT>
                {
                    Success = t.Status == TaskStatus.RanToCompletion,
                    Exception = t.Status == TaskStatus.Faulted ? t.Exception?.InnerException : null,
                    Data = t.Status == TaskStatus.RanToCompletion ? task.Result : default(RT)
                });
            });
        }

        public static Task Done(this Task task, Action<Exception> action)
        {
            return task.OnCompleted((t) => t.Status == TaskStatus.Faulted || t.Status == TaskStatus.RanToCompletion, (t) =>
            {
                action?.Invoke(t.Status == TaskStatus.Faulted ? t.Exception?.InnerException : null);
            });
        }
    }

    public struct TaskDoneInfo<RT>
    {
        public bool Success;
        public RT Data;
        public Exception Exception;
    }
}
