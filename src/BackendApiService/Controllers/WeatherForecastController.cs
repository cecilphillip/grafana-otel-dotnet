using Microsoft.AspNetCore.Mvc;

namespace BackendApiService.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    private readonly Instrumentor _instrumentor;
    private readonly ILogger<WeatherForecastController> _logger;

    public WeatherForecastController(Instrumentor instrumentor, ILogger<WeatherForecastController> logger)
    {
        _instrumentor = instrumentor;
        _logger = logger;
    }

    [HttpGet(Name = "GetWeatherForecast")]
    public IEnumerable<WeatherForecast> Get()
    {
        _logger.LogInformation("Gather weather information");
        _instrumentor.IncomingRequestCounter.Add(1, 
            new KeyValuePair<string, object?>("operation","GetWeatherForecast"),
            new KeyValuePair<string, object?>("controller",nameof(WeatherForecastController)));
        
        return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
    }
}