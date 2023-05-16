using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace BackendApiService;

public sealed class Instrumentor: IDisposable
{
    public const string ServiceName = "BackendApiService";
    
    public ActivitySource Tracer { get; }
    public Meter Recorder { get; }
    public Counter<long> IncomingRequestCounter { get; }

    public Instrumentor()
    {
        var version = typeof(Instrumentor).Assembly.GetName().Version?.ToString();
        Tracer = new ActivitySource(ServiceName, version);
        Recorder = new Meter(ServiceName, version);
        IncomingRequestCounter = Recorder.CreateCounter<long>("app.incoming.requests",
            description: "The number of incoming requests to the backend API");
    }
    
    public void Dispose()
    {
        Tracer.Dispose();
        Recorder.Dispose();
    }
}