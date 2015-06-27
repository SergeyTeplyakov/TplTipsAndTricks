using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading.Tasks;

namespace TplTipsAndTricks.ProcessTasksOneByOne
{
    public static class TaskEx
    {
        public static Task<T> FromTaskResult<T, U>(T result, Func<T, Task<U>> taskSelector)
        {
            Contract.Requires(taskSelector != null);

            var tcs = new TaskCompletionSource<T>();
            var task = taskSelector(result);

            task.ContinueWith(t =>
            {
                if (t.IsFaulted)
                    tcs.SetException(t.Exception);
                else if (t.IsCanceled)
                    tcs.SetCanceled();
                else
                    tcs.SetResult(result);
            });

            return tcs.Task;
        }

        public static async Task ProcessOneByOne<T, U>(
            IEnumerable<T> sequence, Func<T, Task<U>> taskSelector,
            Action<T, U> processor)
        {
            Contract.Requires(sequence != null);
            Contract.Requires(taskSelector != null);
            Contract.Requires(processor != null);

            var tasks = (from element in sequence
                            let pair = new {Element = element, Task = taskSelector(element)}
                            select FromTaskResult(pair, p => p.Task)).ToList();
                        
            while (tasks.Count != 0)
            {
                // Getting the first finished task
                var completedTask = await Task.WhenAny(tasks);

                tasks.Remove(completedTask);

                // Extracting result from finished task
                Contract.Assert(completedTask.IsCompleted, "Task should be completed");
                var result = completedTask.Result;

                // Process element by callback function
                Contract.Assert(result.Task.IsCompleted, "Task should be completed");
                processor(result.Element, result.Task.Result);
            }
        }

        public static async Task ProcessOneByOne<T>(
            IEnumerable<Task<T>> taskSequence, Action<T> processor)
        {
            Contract.Requires(taskSequence != null);
            Contract.Requires(processor != null);

            var tasks = taskSequence.ToList();

            while (tasks.Count != 0)
            {
                // Getting the first finished task
                Task<T> completedTask = await Task.WhenAny(tasks);

                tasks.Remove(completedTask);

                T result = completedTask.Result;

                processor(result);
            }
        }

    }
}