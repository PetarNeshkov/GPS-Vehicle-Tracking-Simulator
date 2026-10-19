namespace GpsTracking.Domain.Models;

public sealed record TrackingAlert(
    Guid Id,
    string VehicleId,
    string Type,
    DateTimeOffset OccurredAt,
    DateTimeOffset CreatedAt,
    double? SpeedKmh,
    double? SpeedLimitKmh,
    int? SpeedingDurationSeconds,
    string? GeofenceId = null,
    string? GeofenceName = null);
