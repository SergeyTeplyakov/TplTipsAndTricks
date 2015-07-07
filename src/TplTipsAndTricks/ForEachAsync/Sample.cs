using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using NUnit.Framework;
using TplTipsAndTricks.Common;
using TplTipsAndTricks.ProcessTasksByCompletion;

namespace TplTipsAndTricks.ForEachAsync
{
    [TestFixture]
    public class Sample
    {
        private Task<Weather> GetWeatherForAsync(string city, bool couldFail = false)
        {
            Console.WriteLine("[{1}]: Getting the weather for '{0}'", city,
                DateTime.Now.ToLongTimeString());

            if (couldFail && (city == "Seattle" || city == "New York"))
            {
                throw new WeatherUnavailableException(city);
            }

            return WeatherService.GetWeatherAsync(city);
        }

        [Test]
        public async Task ForEachAsync()
        {
            var cities = new List<string> { "Moscow", "Seattle", "New York", "Kiev" };

            var tasks = cities.ForEachAsync(async city =>
            {
                return new { City = city, Weather = await GetWeatherForAsync(city) };
            }, 2);

            foreach (var task in tasks)
            {
                var taskResult = await task;

                ProcessWeather(taskResult.City, taskResult.Weather);
            }
        }
        
        [Test]
        public async Task ProcessByCompletionWithFailues()
        {
            var cities = new List<string> { "Moscow", "Seattle", "New York", "Kiev" };

            var tasks = cities.ForEachAsync(async city =>
            {
                return new { City = city, Weather = await GetWeatherForAsync(city, true) };
            }, 2);

            foreach (var task in tasks.OrderByCompletion())
            {
                try
                {
                    var taskResult = await task;
                    ProcessWeather(taskResult.City, taskResult.Weather);
                }
                catch (Exception exception)
                {
                    Console.WriteLine("[{1}]: Failure - '{0}'", exception.Message,
                        DateTime.Now.ToLongTimeString());
                }
            }
        }

        private void ProcessWeather(string city, Weather weather)
        {
            Console.WriteLine("[{2}]: Processing weather for '{0}': '{1}'", city, weather,
                DateTime.Now.ToLongTimeString());
        }
    }
}