using BackendApiService;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;

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
        opts.ResourceAttributes = new Dictionary<string, object>
        {
            ["app"] = "webapi",
            ["runtime"] = "dotnet",
            ["service.name"] = "BackendApiService"
        };
    })
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddSingleton<Instrumentor>();
builder.Services.AddOpenTelemetry()
    .WithTracing(tracerProviderBuilder =>
        tracerProviderBuilder
            .AddSource(Instrumentor.ServiceName)
            .ConfigureResource(resource => resource
                .AddService(Instrumentor.ServiceName))
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation(opts =>
            {
                opts.FilterHttpRequestMessage = req =>
                {
                    var ignore = new[] { "/loki/api" };
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

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();
app.MapControllers();
app.Run();