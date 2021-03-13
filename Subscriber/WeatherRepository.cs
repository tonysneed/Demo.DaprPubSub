using System.Collections.Generic;

namespace Subscriber
{
    public class WeatherRepository
    {
        public List<WeatherForecast> WeatherForecasts { get; set; } = new List<WeatherForecast>();
    }
}
