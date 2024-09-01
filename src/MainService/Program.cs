using System.Reflection;
using MainSerivce.Data;
using MainService;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<Instrumentor>();

builder.Services.ConfigureHttpClientDefaults(http =>
    http.AddStandardResilienceHandler());

builder.Logging.AddOpenTelemetry(logging =>
{
    logging.IncludeFormattedMessage = true;
    logging.IncludeScopes = true;
});

void ConfigureResource(ResourceBuilder resource)
{
    var assembly = Assembly.GetExecutingAssembly().GetName();
    var assemblyVersion = assembly.Version?.ToString() ?? "1.08";
    resource.AddService(Instrumentor.ServiceName, serviceVersion: assemblyVersion)
        .AddTelemetrySdk()
        .AddAttributes(new Dictionary<string, object>
        {
            ["environment.name"] = builder.Environment.EnvironmentName,
            ["app.group"] = "main"
        });
}

builder.Services.AddOpenTelemetry()
    .ConfigureResource(ConfigureResource)
    .WithTracing(tracerProviderBuilder =>
        tracerProviderBuilder
            .AddSource(Instrumentor.ServiceName)
            .AddAspNetCoreInstrumentation(opts =>
            {
                opts.Filter = ctx =>
                {
                    var ignore = new[]
                    {
                        "/_blazor", "/_framework", ".css",
                        "/css", "/favicon"
                    };
                    return !ignore.Any(s => ctx.Request.Path.Value!.Contains(s));
                };
            })
            .AddHttpClientInstrumentation())
    .WithMetrics(metricsProviderBuilder =>
        metricsProviderBuilder
            .AddMeter(Instrumentor.ServiceName)
            .AddRuntimeInstrumentation()
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation())
    .UseOtlpExporter();

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddSingleton<IWeatherService, RemoteWeatherService>();
builder.Services.AddHttpClient("WeatherService", client =>
{
    client.BaseAddress = new Uri("http://localhost:5006");
    client.DefaultRequestHeaders.UserAgent.ParseAdd("MainService");
});

var app = builder.Build();

app.UseStaticFiles();

app.UseRouting(); 
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();