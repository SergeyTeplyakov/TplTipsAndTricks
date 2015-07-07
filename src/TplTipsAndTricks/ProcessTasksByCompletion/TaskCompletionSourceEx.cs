using System;
using System.Diagnostics.Contracts;
using System.Threading.Tasks;

namespace TplTipsAndTricks.ProcessTasksByCompletion
{
    public static class TaskCompletionSourceEx
    {
        
        public static void FromTask<T>(this TaskCompletionSource<T> tcs, Task<T> task)
        {
            tcs.FromTask(task, _ => task.Result);
        }

        public static void FromTask<TResult, TTaskResult>(
            this TaskCompletionSource<TResult> tcs, Task<TTaskResult> task, Func<TTaskResult, TResult> resultSelector)
        {
            // add additional checks
            if (task.Status == TaskStatus.Faulted)
            {
                // Black magic detected: extracting first exception if its the only one!
                var ae = task.Exception;
                var targetException = ae.InnerExceptions.Count == 1 ? ae.InnerExceptions[0] : ae;
                tcs.TrySetException(targetException);
            }
            else if (task.Status == TaskStatus.Canceled)
            {
                tcs.TrySetCanceled();
            }
            else if (task.Status == TaskStatus.RanToCompletion)
            {
                tcs.TrySetResult(resultSelector(task.Result));
            }
            else
            {
                string message = string.Format("Task should be in one of the final states! Current state: '{0}'",
                    task.Status);

                Contract.Assert(false, message);
                throw new InvalidOperationException(message);
            }
        }

        public static void FromTask(
            this TaskCompletionSource<object> tcs, Task task)
        {
            // add additional checks
            if (task.Status == TaskStatus.Faulted)
            {
                // Black magic detected: extracting first exception if its the only one!
                var ae = task.Exception;
                var targetException = ae.InnerExceptions.Count == 1 ? ae.InnerExceptions[0] : ae;
                tcs.TrySetException(targetException);
            }
            else if (task.Status == TaskStatus.Canceled)
            {
                tcs.TrySetCanceled();
            }
            else if (task.Status == TaskStatus.RanToCompletion)
            {
                tcs.TrySetResult(null);
            }
            else
            {
                string message = string.Format("Task should be in one of the final states! Current state: '{0}'",
                    task.Status);

                Contract.Assert(false, message);
                throw new InvalidOperationException(message);
            }
        }
    }
}