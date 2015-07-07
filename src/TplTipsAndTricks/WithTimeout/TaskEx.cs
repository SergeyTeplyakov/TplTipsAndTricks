using System;
using System.Threading.Tasks;
using TplTipsAndTricks.ProcessTasksByCompletion;

namespace TplTipsAndTricks.WithTimeout
{
    public static class TaskEx
    {
        public static Task<T> WithTimeout<T>(this Task<T> task, TimeSpan timeout)
        {
            var tcs = new TaskCompletionSource<T>();

            task.ContinueWith(t =>
            {
                // This method call will guarantee observe tasks exception!
                tcs.FromTask(t);
            }, TaskContinuationOptions.ExecuteSynchronously);

            Task.Delay(timeout)
                .ContinueWith(t => tcs.TrySetCanceled(),
                    TaskContinuationOptions.ExecuteSynchronously);

            return tcs.Task;
        }
        
        public static Task WithTimeout(this Task task, TimeSpan timeout)
        {
            var tcs = new TaskCompletionSource<object>();

            task.ContinueWith(t =>
            {
                // This method call will guarantee observe tasks exception!
                tcs.FromTask(t);
            }, TaskContinuationOptions.ExecuteSynchronously);

            Task.Delay(timeout)
                .ContinueWith(t => tcs.TrySetCanceled(), 
                    TaskContinuationOptions.ExecuteSynchronously);
            
            return tcs.Task;
        }

        /// <summary>
        /// Naive implementation of the 'WithTimeout' idiom.
        /// </summary>
        /// <remarks>
        /// If <paramref name="task"/> will fail after timeout is occurred, UnobservedException could occurr.
        /// </remarks>
        internal static Task WithTimeoutNaive(this Task task, TimeSpan timeout)
        {
            var tcs = new TaskCompletionSource<object>();
            
            Task.WhenAny(task, Task.Delay(timeout))
                .ContinueWith(t =>
                {
                    if (t == task)
                    {
                        tcs.FromTask(task);
                    }
                    else
                    {
                        tcs.TrySetCanceled();
                    }
                });

            return tcs.Task;
        }
    }
}