namespace GpsTracking.Application.Simulation;

public sealed record SimulationSettings(double CruisingSpeedKmh, double SpeedingSpeedKmh,
    int DrivingSeconds, int SpeedingSeconds, int RecoverySeconds, int StopSeconds, int ResumeSeconds)
{
    public const string InvalidSettings = "Simulation speeds must be finite and positive; phase durations must be positive whole seconds.";

    public bool IsValid => double.IsFinite(CruisingSpeedKmh) && CruisingSpeedKmh > 0
        && double.IsFinite(SpeedingSpeedKmh) && SpeedingSpeedKmh > 0
        && DrivingSeconds > 0 && SpeedingSeconds > 0 && RecoverySeconds > 0
        && StopSeconds > 0 && ResumeSeconds > 0;
}
