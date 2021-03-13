using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace Subscriber.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };
        private readonly WeatherRepository _weatherRepo;
        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(WeatherRepository weatherRepo, ILogger<WeatherForecastController> logger)
        {
            _weatherRepo = weatherRepo;
            _logger = logger;
        }

        [HttpGet]
        public IEnumerable<WeatherForecast> Get()
        {
            _logger.LogInformation($"Weatherforecast requested.");
            return _weatherRepo.WeatherForecasts;
        }
    }
}
