using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading.Tasks;

namespace TplTipsAndTricks.ProcessTasksByCompletion
{
    public static class TaskEx
    {
        public static Task<TResult> FromTask<TResult, TTaskResult>(
            TResult result, Func<TResult, Task<TTaskResult>> taskSelector)
        {
            Contract.Requires(taskSelector != null);

            var tcs = new TaskCompletionSource<TResult>();
            var task = taskSelector(result);

            task.ContinueWith(t =>
            {
                tcs.FromTask(task, _ => result);
            });

            return tcs.Task;
        }

        public static IEnumerable<Task<TElement>> OrderByCompletion<TElement, TTaskResult>(
            this IEnumerable<TElement> sequence, Func<TElement, Task<TTaskResult>> taskSelector)
        {
            Contract.Requires(sequence != null);
            Contract.Requires(taskSelector != null);

            var tasks = (from element in sequence
                        let pair = new {Element = element, Task = taskSelector(element)}
                        select FromTask(pair, p => p.Task)).ToList();
                        
            while (tasks.Count != 0)
            {
                var tcs = new TaskCompletionSource<TElement>();

                // Getting the first finished task
                Task.WhenAny(tasks).ContinueWith(tsk =>
                {
                    var finishedTask = tsk.Result;
                    tasks.Remove(finishedTask);

                    tcs.FromTask(finishedTask, arg => arg.Element);
                });

                yield return tcs.Task;
            }
        }

        /// <summary>
        /// Method that transforms sequence of tasks into another sequence of tasks with strict
        /// completion order. I.e. first item in the resulting sequence would be finished first,
        /// second item - second etc. 
        /// </summary>
        /// <remarks>
        /// This method helps to analyze the results not based on the order of the tasks in
        /// the original sequence but in the order of completion.
        /// </remarks>
        public static IEnumerable<Task<T>> OrderByCompletion<T>(this IEnumerable<Task<T>> taskSequence)
        {
            // Need to return task immediately, but it should be in complete state when the first task would be completed!
            Contract.Requires(taskSequence != null);

            var tasks = taskSequence.ToList();

            while (tasks.Count != 0)
            {
                // We whould have additional closure for each iteration but in a task-based world
                // this should be fine!

                var tcs = new TaskCompletionSource<T>();

                // Getting the first finished task
                Task.WhenAny(tasks).ContinueWith((Task<Task<T>> tsk) =>
                {
                    tasks.Remove(tsk.Result);

                    tcs.FromTask(tsk.Result);
                });

                yield return tcs.Task;
            }
        }
    }
}