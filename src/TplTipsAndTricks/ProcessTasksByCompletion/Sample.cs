using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using TplTipsAndTricks.Common;

namespace TplTipsAndTricks.ProcessTasksByCompletion
{
    public static partial class ObservableExtensions
    {
        public static IObservable<TResult> SelectMany<TSource, TResult>(
            this IObservable<TSource> source,
            Func<TSource, CancellationToken, Task<TResult>> selector,
            int maxDegreeOfConcurrency)
        {
            return Observable.Create<TResult>((observer, token) =>
            {
                //var syncedObserver = Subject.(observer);
                var semaphore = new SemaphoreSlim(maxDegreeOfConcurrency);
                var subscription = source.Subscribe(
                    async value =>
                    {
                        try
                        {
                            semaphore.Wait(token);
                        }
                        catch (OperationCanceledException)
                        {
                            return;
                        }
                        catch (Exception ex)
                        {
                            observer.OnError(ex);
                            return;
                        }

                        TResult result;
                        try
                        {
                            result = await selector(value, token).ConfigureAwait(false);
                        }
                        catch (OperationCanceledException ex)
                        {
                            semaphore.Release();
                            if (ex.CancellationToken != token)
                            {
                                observer.OnError(ex);
                            }

                            return;
                        }
                        catch (Exception ex)
                        {
                            semaphore.Release();
                            observer.OnError(ex);

                            return;
                        }

                        semaphore.Release();
                        observer.OnNext(result);
                    },
                    observer.OnError,
                    observer.OnCompleted);

                return Task.FromResult(subscription);
            }).Synchronize();
        }
    }

    [TestFixture]
    public class Sample
    {
        private Task<Weather> GetWeatherForAsync(string city)
        {
            Console.WriteLine("[{1}]: Getting the weather for '{0}'", city,
                DateTime.Now.ToLongTimeString());
            return WeatherService.GetWeatherAsync(city);
        }

        [Test]
        public async Task ManualProcessByCompletion()
        {
            var cities = new List<string> { "Moscow", "Seattle", "New York" };
            var tasks = cities.Select(async city =>
            {
                return new {City = city, Weather = await GetWeatherForAsync(city)};
            }).ToList();

            while (tasks.Count != 0)
            {
                var completedTask = await Task.WhenAny(tasks);

                tasks.Remove(completedTask);

                var result = completedTask.Result;

                ProcessWeather(result.City, result.Weather);
            }
        }

        [Test]
        public async Task ProcessByCompletionWithQueryComprehension()
        {
            var cities = new List<string> { "Moscow", "Seattle", "New York" };
            
            var tasks =
                from city in cities
                select new {City = city, WeatherTask = GetWeatherForAsync(city)};

            foreach (var task in tasks.OrderByCompletion(t => t.WeatherTask))
            {
                var taskResult = await task;

                // taskResult is an object of anonymous type with City and WeatherTask
                ProcessWeather(taskResult.City, taskResult.WeatherTask.Result);
            }
        }
        
        [Test]
        public async Task ProcessByCompletion()
        {
            var cities = new List<string> { "Moscow", "Seattle", "New York" };

            var tasks = cities.Select(async city =>
            {
                return new {City = city, Weather = await GetWeatherForAsync(city)};
            });

            foreach (var task in tasks.OrderByCompletion())
            {
                var taskResult = await task;

                // taskResult is an object of anonymous type with City and WeatherTask
                ProcessWeather(taskResult.City, taskResult.Weather);
            }
        }

        [Test]
        public void ProcessOneUsingRx()
        {
            var cities = new[] { "Moscow", "Seattle", "New York" };
            var objs = cities.Select(async city => new
            {
                City = city,
                Weather = await GetWeatherForAsync(city)
            }).Select(task => task.ToObservable()).Merge().ToEnumerable();

            foreach (var obj in objs)
            {
                ProcessWeather(obj.City, obj.Weather);
            }
        }

[Test]
public async Task ProcessOneByOneNaive()
{
var cities = new List<string> { "Moscow", "Seattle", "New York" };

var tasks =
    from city in cities
    select new { City = city, WeatherTask = GetWeatherForAsync(city) };

foreach (var entry in tasks)
{
    var wheather = await entry.WeatherTask;

    ProcessWeather(entry.City, wheather);
}
}

private void ProcessWeather(string city, Weather weather)
{
    Console.WriteLine("[{2}]: Processing weather for '{0}': '{1}'", city, weather,
        DateTime.Now.ToLongTimeString());
}
    }
}