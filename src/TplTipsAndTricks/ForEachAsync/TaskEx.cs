using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TplTipsAndTricks.ForEachAsync
{
    public static class TaskEx
    {
        public static Task ForEachAsync<T>(this IEnumerable<T> sequence, Func<T, Task> selector, int degreeOfParallelism)
        {
            return null;
        }
        
        public static IEnumerable<Task<U>> ForEachAsync<T, U>(this IEnumerable<T> sequence, Func<T, Task<U>> selector, int degreeOfParallelism)
        {
            return null;
        }
    }
}