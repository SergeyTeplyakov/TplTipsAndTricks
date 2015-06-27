using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace TplTipsAndTricks.ProcessTasksOneByOne
{
    class Weather
    {
        public Weather(int temperatureCelcius)
        {
            TemperatureCelcius = temperatureCelcius;
        }

        public int TemperatureCelcius { get; private set; }

        public override string ToString()
        {
            return string.Format("Temp: {0}C", TemperatureCelcius);
        }
    }

    internal static class WeatherService
    {

        public static Task<Weather> GetWeatherAsync(string city)
        {
            return Task.Run(
                async () =>
                {
                    await Task.Yield();
                    Console.WriteLine("Starting getting the weather for '{0}'", city);
                    
                    // Faking the temerature by city name length:)
                    
                    // Each task should take random amount of time
                    var interval = 1 + new Random(Guid.NewGuid().GetHashCode()).Next(7);
                    Console.WriteLine("Sleeping for {0}sec", interval);
                    
                    await Task.Delay(TimeSpan.FromSeconds(interval));

                    var result = new Weather(city.Length);
                    Console.WriteLine("Got the weather for '{0}'", city);
                    return result;
                });
        }
    }
    [TestFixture]
    public class ProcessTasksOneByOneSample
    {
        private Task<Weather> GetWeatherForAsync(string city)
        {
            Task.Yield();
            Console.WriteLine("Getting the weather for '{0}'", city);
            return WeatherService.GetWeatherAsync(city);
        }

        [Test]
        public async Task ManualProcessOneByOne()
        {
            var cities = new List<string> { "Moscow", "Seattle", "New York" };
            var tasks = (from city in cities
                         let result = new { City = city, WeatherTask = GetWeatherForAsync(city) }
                         select TaskEx.FromTaskResult(result, r => r.WeatherTask)).ToList();

            while (tasks.Count != 0)
            {
                var completedTask = await Task.WhenAny(tasks);

                tasks.Remove(completedTask);

                var result = completedTask.Result;

                ProcessWeather(result.City, result.WeatherTask.Result);
            }
        }

        [Test]
        public async Task TaskExProcessOneByOne()
        {
            var cities = new List<string> { "Moscow", "Seattle", "New York" };
            
            var tasks =
                from city in cities
                select new {City = city, WeatherTask = GetWeatherForAsync(city)};

            await TaskEx.ProcessOneByOne(
                tasks, t => t.WeatherTask,
                (result, weather) => ProcessWeather(result.City, weather));
        }

        private void ProcessWeather(string city, Weather weather)
        {
            Console.WriteLine("[{2}]: Processing weather for '{0}': '{1}'", city, weather,
                DateTime.Now.ToLongTimeString());
        } 
    }
}