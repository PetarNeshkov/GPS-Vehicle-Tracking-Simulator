using GpsTracking.Domain.Constants;

namespace GpsTracking.Domain.Models;

public sealed record VehicleSettings
{
    public double SpeedLimitKmh { get; init; } = SettingsConstants.DefaultSpeedLimitKmh;
    public int SpeedingDurationSeconds { get; init; } = SettingsConstants.DefaultSpeedingDurationSeconds;
    public int StopDurationSeconds { get; init; } = SettingsConstants.DefaultStopDurationSeconds;

    public string? Validate()
        => double.IsFinite(SpeedLimitKmh) && SpeedLimitKmh > 0
        && SpeedingDurationSeconds > 0 && StopDurationSeconds > 0
        ? null : SettingsConstants.InvalidVehicleSettings;
}
