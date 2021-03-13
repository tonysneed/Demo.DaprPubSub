using Dapr;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

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
        public IActionResult PostWeathers(WeatherForecast weather)
        {
            _logger.LogInformation($"Weather posted.");
            _weatherRepo.WeatherForecasts.Add(weather);
            return NoContent();
        }
    }
}
