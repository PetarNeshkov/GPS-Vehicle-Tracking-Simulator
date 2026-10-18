namespace GpsTracking.Domain.Models;

public sealed record VehicleDefinition
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public VehicleSettings Settings { get; init; } = new();
}
