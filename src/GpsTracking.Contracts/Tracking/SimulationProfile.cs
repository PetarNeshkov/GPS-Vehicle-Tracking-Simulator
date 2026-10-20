namespace GpsTracking.Contracts.Tracking;

public sealed record SimulationProfile(double CruisingSpeedKmh, double SpeedingSpeedKmh,
    int DrivingSeconds, int SpeedingSeconds, int RecoverySeconds, int StopSeconds, int ResumeSeconds)
{
    public static SimulationProfile DefaultFor(int vehicleIndex) => vehicleIndex switch
    {
        0 => new(42, 95, 120, 60, 30, 210, 120),
        1 => new(66, 95, 330, 60, 30, 210, 90),
        _ => throw new ArgumentOutOfRangeException(nameof(vehicleIndex))
    };
}
