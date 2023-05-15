using System.Diagnostics;

namespace BackendApiService;

public static class DiagnosticsConfig
{
    public const string ServiceName = "BackendApiService";
    public static readonly ActivitySource ActivitySource = new(ServiceName);
}