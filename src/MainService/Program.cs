using MainSerivce.Data;
using MainService;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Formatting.Display;
using Serilog.Sinks.Grafana.Loki;

var builder = WebApplication.CreateBuilder(args);

const string outputTemplate =
    "[{Level:w}]: {Timestamp:dd-MM-yyyy:HH:mm:ss} {MachineName} {EnvironmentName} {SourceContext} {Message}{NewLine}{Exception}";

var formatter = new MessageTemplateTextFormatter(outputTemplate);
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .Enrich.WithThreadId()
    .Enrich.WithEnvironmentName()
    .Enrich.WithMachineName()
    .WriteTo.Console(outputTemplate: outputTemplate)
    .WriteTo.GrafanaLoki("http://localhost:3100",
        new List<LokiLabel>
        {
            new() { Key = "app", Value = "web" },
            new() { Key = "runtime", Value = "dotnet" }
        },
        period: TimeSpan.FromSeconds(1),
        textFormatter: formatter,
        propertiesAsLabels: new[] { "EnvironmentName", "MachineName", "level" })
    .CreateLogger();

builder.Host.UseSerilog();
builder.Services.AddSingleton<Instrumentor>();
builder.Services.AddOpenTelemetry()
    .WithTracing(tracerProviderBuilder =>
        tracerProviderBuilder
            .AddSource(Instrumentor.ServiceName)
            .ConfigureResource(resource => resource
                .AddService(Instrumentor.ServiceName))
            .AddAspNetCoreInstrumentation(opts =>
            {
                opts.Filter = ctx =>
                {
                    var ignore = new[]
                    {
                        "/_blazor", "/_framework",
                        "/css", "/swagger", "/favicon"
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
            .ConfigureResource(resource => resource
                .AddService(Instrumentor.ServiceName))
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