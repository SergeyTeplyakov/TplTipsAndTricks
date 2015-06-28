using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using TplTipsAndTricks.Common;

namespace TplTipsAndTricks.ProcessTasksByCompletion
{
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
            var tasks = (from city in cities
                            let result = new { City = city, WeatherTask = GetWeatherForAsync(city) }
                            select TaskEx.FromTask(result, r => r.WeatherTask)).ToList();

            while (tasks.Count != 0)
            {
                var completedTask = await Task.WhenAny(tasks);

                tasks.Remove(completedTask);

                var result = completedTask.Result;

                ProcessWeather(result.City, result.WeatherTask.Result);
            }
        }

        [Test]
        public async Task ProcessByCompletion()
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