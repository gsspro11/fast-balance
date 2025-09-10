using System.Diagnostics.Metrics;

namespace Api;

public static class DiagnosticsConfig
{
    public const string ServiceName = "Statement";

    public static readonly Meter Meter = new(ServiceName);

    public static Counter<int> SalesCounter = Meter.CreateCounter<int>("sales.count");
}
