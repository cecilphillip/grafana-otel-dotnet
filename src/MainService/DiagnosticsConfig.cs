using System.Diagnostics;

namespace MainService;

public static class DiagnosticsConfig
{
    public const string ServiceName = "MainService";
    public static readonly ActivitySource ActivitySource = new(ServiceName);
}