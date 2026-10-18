namespace GpsTracking.Domain.Models;

public sealed record VehicleSettings
{
    public double SpeedLimitKmh { get; init; } = 80;
    public int SpeedingDurationSeconds { get; init; } = 30;
    public int StopDurationSeconds { get; init; } = 180;

    // Provisional default pending evaluator confirmation.
    public double StopRadiusMeters { get; init; } = 10;
}
