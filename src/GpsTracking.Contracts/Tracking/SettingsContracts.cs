using System.ComponentModel.DataAnnotations;

namespace GpsTracking.Contracts.Tracking;

public sealed record VehicleSettingsResponse(double SpeedLimitKmh, int SpeedingDurationSeconds, int StopDurationSeconds);

public sealed record GlobalSettingsResponse(double StopRadiusMeters, long Revision);

public sealed record GlobalSettingsEvent(Guid SessionId, GlobalSettingsResponse Settings);

public sealed record AlertResponse(Guid Id,
    string VehicleId,
    string Type,
    DateTimeOffset OccurredAt,
    DateTimeOffset CreatedAt,
    double? SpeedKmh,
    double? SpeedLimitKmh,
    int? SpeedingDurationSeconds,
    string? GeofenceId = null,
    string? GeofenceName = null);

public sealed record AlertsResponse(Guid SessionId, IReadOnlyList<AlertResponse> Alerts);

public sealed record UpdateVehicleSettingsRequest
{
    [Required]
    public double? SpeedLimitKmh { get; init; }

    [Required]
    public int? SpeedingDurationSeconds { get; init; }

    [Required]
    public int? StopDurationSeconds { get; init; }
}

public sealed record UpdateGlobalSettingsRequest
{
    [Required]
    public double? StopRadiusMeters { get; init; }
}
