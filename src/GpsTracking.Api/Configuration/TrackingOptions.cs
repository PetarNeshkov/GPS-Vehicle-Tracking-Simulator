using GpsTracking.Domain.Models;

namespace GpsTracking.Api.Configuration;

public sealed class TrackingOptions
{
    public const string SectionName = "Tracking";

    public VehicleDefinition[] Vehicles { get; init; } = [];
    public GeofenceDefinition[] Geofences { get; init; } = [];
}
