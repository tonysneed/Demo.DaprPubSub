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
   - Add the `` NuGet package.
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

6. Run the Web API via Dapr from the `Subscriber` project root.
   ```
   dapr run --app-id subscriber --app-port 5000 -- dotnet run
   ```
   - Refresh the Swagger page and execute `GET` for `Weatherforecast`.
   - Run `dapr dashboard` from a terminal, then browse to http://localhost:8080.

