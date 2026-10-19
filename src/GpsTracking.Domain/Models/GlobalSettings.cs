using GpsTracking.Domain.Constants;

namespace GpsTracking.Domain.Models;

public sealed record GlobalSettings
{
    public double StopRadiusMeters { get; init; } = SettingsConstants.DefaultStopRadiusMeters;

    public string? Validate() => double.IsFinite(StopRadiusMeters) && StopRadiusMeters > 0
        ? null : SettingsConstants.InvalidGlobalSettings;
}
