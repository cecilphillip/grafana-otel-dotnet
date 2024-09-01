using System.Reflection;
using BackendApiService;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<Instrumentor>();
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
                opts.Filter = context =>
                {
                    var ignore = new[] { "/swagger" };
                    return !ignore.Any(s => context.Request.Path.ToString().Contains(s));
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

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.Run();