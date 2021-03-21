using Dapr.Client;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Publisher
{
    public class Worker : BackgroundService
    {
        private readonly WeatherFactory _factory;
        private readonly DaprClient _daprClient;
        private readonly ILogger<Worker> _logger;

        public Worker(WeatherFactory factory, DaprClient daprClient, ILogger<Worker> logger)
        {
            _factory = factory;
            _daprClient = daprClient;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Publishing event at: {time}", DateTimeOffset.Now);

                var weather = _factory.CreateWeather();
                await _daprClient.PublishEventAsync(Constants.PubSubName, "weather", weather);

                await Task.Delay(5000, stoppingToken);
            }
        }
    }
}
