using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace TplTipsAndTricks.WithTimeout
{
    [TestFixture]
    public class Sample
    {
        public static async Task FooAsync(CancellationToken token)
        {
            
        }

        [Test]
        public async Task UseTaskCancellationSourceForTimeout()
        {
            try
            {
                TimeSpan timeout = TimeSpan.FromSeconds(1);
                var cts = new CancellationTokenSource(timeout);

                await Task.Delay(TimeSpan.FromSeconds(5), cts.Token);
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine("Task was successfully cancelled!");
            }
        }

        [Test]
        public async Task UseWithTimeout()
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(5))
                    .WithTimeout(TimeSpan.FromSeconds(1));
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine("Task was successfully cancelled!");
            }            
        }

        private static async Task LongRunningThatFail(int timeout)
        {
            await Task.Delay(timeout);
            throw new Exception("Long running operation failed!");
        }

        private static async Task AsyncMethodWithTimeout(bool useNaive)
        {
            const int timeout = 500;
            {
                var task = LongRunningThatFail(timeout);

                try
                {
                    if (useNaive)
                    {
                        await task.WithTimeoutNaive(TimeSpan.FromMilliseconds(100));
                    }
                    else
                    {
                        await task.WithTimeout(TimeSpan.FromMilliseconds(100));
                    }
                    Assert.Fail("Should never get here! Previous line should always throw!");
                }
                catch (TaskCanceledException)
                {
                    Console.WriteLine("Expected timeout!");
                }

                // Additional timeout. Just in case
                Thread.Sleep(400);
            }
        }

        [Test]
        public void NaiveImplementationShouldThrow()
        {
            TaskScheduler.UnobservedTaskException += (sender, args) =>
            {
                Console.WriteLine("Unobserved exception: " + args.Exception);
                
            };

            {
                var tsk = AsyncMethodWithTimeout(useNaive: true);
                tsk.Wait();
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                Console.WriteLine("Done!");
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            Console.WriteLine("Done!");
        }
        
        [Test]
        public void WithTimeoutShouldNotThrowUnobservedException()
        {
            TaskScheduler.UnobservedTaskException += (sender, args) =>
            {
                Console.WriteLine("Unobserved exception: " + args.Exception);
                
            };

            {
                var tsk = AsyncMethodWithTimeout(useNaive: false);
                tsk.Wait();
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                Console.WriteLine("Done!");
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            Console.WriteLine("Done!");
        }
    }
}