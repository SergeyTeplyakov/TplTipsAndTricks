using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TplTipsAndTricks.ProcessTasksByCompletion;

namespace TplTipsAndTricks.ForEachAsync
{
    public static class TaskEx
    {
        public static IEnumerable<Task<TTask>> ForEachAsync<TItem, TTask>(
            this IEnumerable<TItem> source, Func<TItem, Task<TTask>> selector, 
            int degreeOfParallelism)
        {
            Contract.Requires(source != null);
            Contract.Requires(selector != null);

            // We need to know all the items in the source before starting tasks
            var tasks = source.ToList();
            
            int completedTask = -1;

            // Creating an array of TaskCompletionSource that would holds
            // the results for each operations
            var taskCompletions = new TaskCompletionSource<TTask>[tasks.Count];
            for(int n = 0; n < taskCompletions.Length; n++) 
                taskCompletions[n] = new TaskCompletionSource<TTask>();

            // Partitioner would do all grunt work for us and split
            // the source into appropriate number of chunks for parallel processing
            foreach (var partition in 
                Partitioner.Create(tasks).GetPartitions(degreeOfParallelism))
            {
                var p = partition;
                
                // Loosing sync context and starting asynchronous 
                // computation for each partition
                Task.Run(async () =>
                {
                    while (p.MoveNext())
                    {
                        var task = selector(p.Current);
                        
                        // Don't want to use empty catch . 
                        // This trick just swallows an exception
                        await task.ContinueWith(_ => { });

                        int finishedTaskIndex = Interlocked.Increment(ref completedTask);
                        taskCompletions[finishedTaskIndex].FromTask(task);
                    }
                });
            }

            return taskCompletions.Select(tcs => tcs.Task);
        }

        /// <summary>
        /// Implementation by Stephen Toub. Found at: http://blogs.msdn.com/b/pfxteam/archive/2012/03/05/10278165.aspx
        /// </summary>
        public static Task ForEachAsync<T>(this IEnumerable<T> source, int dop, Func<T, Task> body)
        {
            return Task.WhenAll(
                from partition in Partitioner.Create(source).GetPartitions(dop)
                select Task.Run(async delegate
                {
                    using (partition)
                        while (partition.MoveNext())
                            await body(partition.Current);
                }));
        }
        
        /// <summary>
        /// Implementation by Stephen Toub. Found at: http://blogs.msdn.com/b/pfxteam/archive/2012/03/05/10278165.aspx
        /// </summary>
        public static async Task ForEachAsyncWithExceptions<T>(this IEnumerable<T> source, int dop, Func<T, Task> body)
        {
            ConcurrentQueue<Exception> exceptions = null;
            
            await Task.WhenAll(
                from partition in Partitioner.Create(source).GetPartitions(dop)
                select Task.Run(async delegate
                {
                    using (partition)
                    {
                        while (partition.MoveNext())
                        {
                            try
                            {
                                await body(partition.Current);
                            }
                            catch (Exception e)
                            {
                                LazyInitializer.EnsureInitialized(ref exceptions).Enqueue(e);
                            }
                        }
                    }
                }));

            if (exceptions != null)
            {
                throw new AggregateException(exceptions);
            }
        }

    }
}