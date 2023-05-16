using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace MainService;

public class Instrumentor : IDisposable
{
    public const string ServiceName = "MainService";

    public Instrumentor()
    {
        var version = typeof(Instrumentor).Assembly.GetName().Version?.ToString();
        Tracer = new ActivitySource(ServiceName, version);
        Recorder = new Meter(ServiceName, version);
        OutgoingRequestCounter = Recorder.CreateCounter<long>("app.outing.requests",
            description: "The number of outgoing backend API requests from the MainService");
    }

    public ActivitySource Tracer { get; }
    public Meter Recorder { get; }
    public Counter<long> OutgoingRequestCounter { get; }

    public void Dispose()
    {
        Tracer.Dispose();
        Recorder.Dispose();
    }
}