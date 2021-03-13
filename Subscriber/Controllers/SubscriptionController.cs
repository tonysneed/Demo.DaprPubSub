using Dapr;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;

namespace Subscriber.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SubscriptionController : ControllerBase
    {
        private readonly WeatherRepository _weatherRepo;
        private readonly ILogger<SubscriptionController> _logger;

        public SubscriptionController(WeatherRepository weatherRepo, ILogger<SubscriptionController> logger)
        {
            _weatherRepo = weatherRepo;
            _logger = logger;
        }

        [Topic("pubsub", "weather")]
        [HttpPost("/weather")]
        public IActionResult PostWeathers(IEnumerable<WeatherForecast> weather)
        {
            _logger.LogInformation($"Weather posted.");
            _weatherRepo.WeatherForecasts = weather.ToList();
            return NoContent();
        }
    }
}
