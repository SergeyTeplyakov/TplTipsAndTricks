using System;
using System.Threading.Tasks;

namespace TplTipsAndTricks.Common
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

}