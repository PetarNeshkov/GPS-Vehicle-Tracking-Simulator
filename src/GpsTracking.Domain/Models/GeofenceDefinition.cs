namespace GpsTracking.Domain.Models;

public sealed record GeofenceDefinition
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public double MinLatitude { get; init; }
    public double MaxLatitude { get; init; }
    public double MinLongitude { get; init; }
    public double MaxLongitude { get; init; }
}
