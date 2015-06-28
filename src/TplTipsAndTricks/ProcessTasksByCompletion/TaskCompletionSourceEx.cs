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
                tcs.SetException(task.Exception);
            }
            else if (task.Status == TaskStatus.Canceled)
            {
                tcs.SetCanceled();
            }
            else if (task.Status == TaskStatus.RanToCompletion)
            {
                tcs.SetResult(resultSelector(task.Result));
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