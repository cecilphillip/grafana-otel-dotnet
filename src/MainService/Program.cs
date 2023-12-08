using MainSerivce.Data;
using MainService;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Sinks.OpenTelemetry;

var builder = WebApplication.CreateBuilder(args);

const string outputTemplate =
    "[{Level:w}]: {Timestamp:dd-MM-yyyy:HH:mm:ss} {MachineName} {EnvironmentName} {SourceContext} {Message}{NewLine}{Exception}";

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .Enrich.WithThreadId()
    .Enrich.WithEnvironmentName()
    .Enrich.WithMachineName()
    .WriteTo.Console(outputTemplate: outputTemplate)
    .WriteTo.OpenTelemetry(opts =>
    {
        opts.IncludedData = IncludedData.SpecRequiredResourceAttributes;
        opts.ResourceAttributes = new Dictionary<string, object>
        {
            ["app"] = "web",
            ["runtime"] = "dotnet",
            ["service.name"] = "MainService"
        };
    })
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddSingleton<Instrumentor>();
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(Instrumentor.ServiceName))
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
            .AddHttpClientInstrumentation(opts =>
            {
                var ignore = new[] { "/loki/api" };

                opts.FilterHttpRequestMessage = req =>
                {
                    return !ignore.Any(s => req.RequestUri!.ToString().Contains(s));
                };
            })
            .AddOtlpExporter())
    .WithMetrics(metricsProviderBuilder =>
        metricsProviderBuilder
            .AddMeter(Instrumentor.ServiceName)
            .AddRuntimeInstrumentation()
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation().AddOtlpExporter());

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
app.UseSerilogRequestLogging();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();