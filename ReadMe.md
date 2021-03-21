# Dapr .NET Pub Sub Demo

Demonstrates how to use Dapr for pub/sub with .NET.

### Prerequisites
- Install [Docker Desktop](https://www.docker.com/products/docker-desktop) running Linux containers.
- Install [Dapr CLI](https://docs.dapr.io/getting-started/install-dapr-cli/).
- Run `dapr init`, `dapr --version`, `docker ps`
- [Dapr Visual Studio Extension](https://github.com/microsoft/vscode-dapr) (for debugging).

### References
- [Dapr Client .NET SDK Docs](https://docs.dapr.io/developing-applications/sdks/dotnet/dotnet-client/).
- [Dapr .NET SDK Repo](https://github.com/dapr/dotnet-sdk).
- [Dapr Pub-Sub Quickstart](https://github.com/dapr/quickstarts/tree/v1.0.0/pub-sub).
- [Dapr ASP.NET Core Controllers Sample](https://github.com/dapr/dotnet-sdk/tree/master/examples/AspNetCore/ControllerSample).
- [Dapr .NETService Invocation example](https://github.com/dapr/dotnet-sdk/tree/master/examples/Client/ServiceInvocation).

### Subscriber

1. Create a Web API project.
   - Disable HTTPS, enable Open API.
   - Add the `Dapr.AspNetCore` NuGet package.
2. Add a `WeatherRepository` class.
   ```csharp
   public class WeatherRepository
   {
      public List<WeatherForecast> WeatherForecasts { get; set; } = new List<WeatherForecast>();
   }
   ```
   - Update `Startup.ConfigureServices` to register it with DI.

   ```csharp
   services.AddSingleton<WeatherRepository>();
   ```
3. Inject `WeatherRepository` into the ctor of `WeatherForecastController`.
   - Then refactor the `Get` method to use it.
   ```csharp
   public WeatherForecastController(WeatherRepository weatherRepo, ILogger<WeatherForecastController> logger)
   {
      _weatherRepo = weatherRepo;
      _logger = logger;
   }

   [HttpGet]
   public IEnumerable<WeatherForecast> Get()
   {
      return _weatherRepo.WeatherForecasts;
   }
   ```
4. Add an empy API `SubscriptionController` to the `Controllers` folder.
   - Inject `WeatherRepository` into the ctor.
   - Add a `PostWeathers` method to the controller.
   - Include `[Topic]` and `HttpPost` attributes.
   ```csharp
   [Route("api/[controller]")]
   [ApiController]
   public class SubscriptionController : ControllerBase
   {
      private readonly WeatherRepository _weatherRepo;

      public SubscriptionController(WeatherRepository weatherRepo)
      {
         _weatherRepo = weatherRepo;
      }

      [Topic("pubsub", "weather")]
      [HttpPost("/weather")]
      public IActionResult PostWeathers(IEnumerable<WeatherForecast> weathers)
      {
         _weatherRepo.WeatherForecasts = weathers.ToList();
         return NoContent();
      }
   }
   ```
5. Update the `Startup` class to enable Dapr integration.
   - Append `.AddDapr()` to `services.AddControllers()`.
   ```csharp
   public void ConfigureServices(IServiceCollection services)
   {
      // Add Dapr integration
      services.AddControllers().AddDapr();
   ```
   - Update `Configure` method to use Cloud Events and map subscribe handler.
   ```csharp
   // Use Cloud Events
   app.UseCloudEvents();

   app.UseEndpoints(endpoints =>
   {
      endpoints.MapControllers();

      // Map subscriber handler
      endpoints.MapSubscribeHandler();
   });
   ```

6. Run the Subscriber via Dapr from the `Subscriber` project root.
   ```
   dapr run --app-id subscriber --app-port 5000 -- dotnet run
   ```
   - Refresh the Swagger page and execute `GET` for `Weatherforecast`.
   - Run `dapr dashboard` from a terminal, then browse to http://localhost:8080.

### Publisher

1. Create a Worker Service project.
2. Add the `Dapr.Client` NuGet package.
3. Copy the `WeatherForecast` class from the Subscriber project.
4. Add a `WeatherFactory` class.
   ```csharp
   public class WeatherFactory
   {
      private readonly string[] Summaries = new[]
      {
         "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
      };

      public List<WeatherForecast> CreateWeather()
      {
         var rng = new Random();
         return Enumerable.Range(1, 5).Select(index => new WeatherForecast
         {
               Date = DateTime.Now.AddDays(index),
               TemperatureC = rng.Next(-20, 55),
               Summary = Summaries[rng.Next(Summaries.Length)]
         })
         .ToList();
      }
   }
   ```
5. Update `ConfigureServices` in `Program.CreateHostBuilder`.
   - Register `WeatherFactory`.
   - Register `DaprClient`.
   ```csharp
   public static IHostBuilder CreateHostBuilder(string[] args) =>
      Host.CreateDefaultBuilder(args)
         .ConfigureServices((hostContext, services) =>
         {
               services.AddHostedService<Worker>();
               services.AddSingleton<WeatherFactory>();
               services.AddSingleton(provider =>  new DaprClientBuilder().Build());
         });
   ```
6. Inject `WeatherFactory` and `DaprClient` into the `Worker` ctor.
   ```csharp
   public Worker(WeatherFactory factory, DaprClient daprClient, ILogger<Worker> logger)
   {
      _factory = factory;
      _daprClient = daprClient;
      _logger = logger;
   }
   ```
7. Update `ExecuteAsync` to publish an event to the `weather` topic.
   - Increase the delay interval to 5 seconds.
   ```csharp
   protected override async Task ExecuteAsync(CancellationToken stoppingToken)
   {
      while (!stoppingToken.IsCancellationRequested)
      {
         _logger.LogInformation("Publishing event at: {time}", DateTimeOffset.Now);

         var weather = _factory.CreateWeather();
         await _daprClient.PublishEventAsync("pubsub", "weather", weather);

         await Task.Delay(5000, stoppingToken);
      }
   }
   ```
8. Run the Publisher via Dapr from the `Publisher` project root.
   ```
   dapr run --app-id publisher -- dotnet run
   ```
   - Go to the Swagger page for the subscriber and execute `GET` for `Weatherforecast`.
   - Repeat every few seconds to see new values.

## Components

Instead of using the default Redis component fo pub/sub, we will now use the [AWS SNS+SQS](https://docs.dapr.io/operations/components/setup-pubsub/supported-pubsub/setup-aws-snssqs/) Dapr component with [LocalStack](https://github.com/localstack/localstack). Have a look at the **pubsub.yaml** file in the **dapr/components** folder.
```yaml
apiVersion: dapr.io/v1alpha1
kind: Component
metadata:
  name: snssqs-pubsub
spec:
  type: pubsub.snssqs
  version: v1
  metadata:
    - name: endpoint
      value: http://localhost:4566
    # Use us-east-1 for localstack
    - name: region
      value: us-east-1
```

1. Run LocalStack using Docker.
   ```
   docker run --rm -p 4566:4566 -p 4571:4571 localstack/localstack
   ```
2. In both **Publisher** and **Subscriber** projects, update the `Constants` class to sspecify "snssqs-pubsub" for `PubSubName`.
   ```csharp
   public static class Constants
   {
      //public const string PubSubName = "pubsub";
      public const string PubSubName = "snssqs-pubsub";
   }
   ```
3. Run the Subscriber service and specify the components path.
   ```
   dapr run --app-id subscriber --app-port 5000 --components-path ../dapr/components -- dotnet run
   ```
4. Run the Publisher service and specify the components path.
   ```
   dapr run --app-id publisher --components-path ../dapr/components -- dotnet run
   ```