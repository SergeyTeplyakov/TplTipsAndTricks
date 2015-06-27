using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using NUnit.Framework;

namespace TplTipsAndTricks._01.ProcessTasksOneByOne
{
    class Weather
    {
        public readonly int TemperatureCelcius;

        public Weather(int temperatureCelcius)
        {
            TemperatureCelcius = temperatureCelcius;
        }
    
        public override string ToString()
        {
            return string.Format("Temp: {0}C°", TemperatureCelcius);
        }
    }

    static class WeatherService
    {
        public static Task<Weather> GetWeatherAsync(string city)
        {
            return Task.Run(async () =>
            {
                await Task.Yield();

                Console.WriteLine("Starting getting the weather for '{0}'", city);
                    
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
        static Task<Weather> GetWeatherForAsync(string city)
        {
            Console.WriteLine("Getting the weather for '{0}'", city);
            return WeatherService.GetWeatherAsync(city);
        }

        [Test]
        public async Task ProcessOneByOne()
        {
            var cities = new List<string> { "Moscow", "Seattle", "New York" };
            
            foreach (var city in cities)
            {
                var weather = await GetWeatherForAsync(city);
                ProcessWeather(city, weather);
            }
        }

        static void ProcessWeather(string city, Weather weather)
        {
            Console.WriteLine("[{2}]: Processing weather for '{0}': '{1}'", 
                              city, weather, DateTime.Now.ToLongTimeString());
        } 
    }
}