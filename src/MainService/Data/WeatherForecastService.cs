using MainService;

namespace MainSerivce.Data;

public class LocalWeatherService : IWeatherService
{
    private static readonly string[] Summaries =
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    public Task<WeatherForecast[]> GetForecastAsync()
    {
        return Task.FromResult(Enumerable.Range(1, 8).Select(index => new WeatherForecast
        {
            Date = DateOnly.FromDateTime(DateTime.Now),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = Summaries[Random.Shared.Next(Summaries.Length)]
        }).ToArray());
    }
}

public class RemoteWeatherService : IWeatherService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly Instrumentor _instrumentor;
    private readonly ILogger<RemoteWeatherService> _logger;

    public RemoteWeatherService(IHttpClientFactory httpClientFactory, Instrumentor instrumentor,
        ILogger<RemoteWeatherService> logger)
    {
        (_httpClientFactory, _instrumentor, _logger) = (httpClientFactory, instrumentor, logger);
    }

    public async Task<WeatherForecast[]> GetForecastAsync()
    {
        using var client = _httpClientFactory.CreateClient("WeatherService");
        _instrumentor.OutgoingRequestCounter.Add(1,
            new KeyValuePair<string, object?>("operation", nameof(GetForecastAsync)));

        var results = await client.GetFromJsonAsync<WeatherForecast[]>("WeatherForecast");
        return results ?? Array.Empty<WeatherForecast>();
    }
}

public interface IWeatherService
{
    Task<WeatherForecast[]> GetForecastAsync();
}