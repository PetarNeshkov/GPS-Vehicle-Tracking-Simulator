using GpsTracking.Contracts.Tracking;

namespace GpsTracking.Simulator.Movement;

public static class DemoScenarios
{
    // Demonstration values, independent of backend settings. All durations are real seconds.
    public const double ReferenceSpeedLimitKmh = 80;
    public const int ReferenceSpeedingDurationSeconds = 30;
    public const int ReferenceStopDurationSeconds = 180;
    public const int CoverageMarginIntervals = 2;

    public static ScenarioProgressor Create(int vehicleIndex)
        => new(DemoRoutes.Create(vehicleIndex), PhasesFor(vehicleIndex));

    public static IReadOnlyList<ScenarioPhase> FromProfile(SimulationProfile profile) => Array.AsReadOnly([
        new ScenarioPhase(ScenarioPhaseKind.Driving, profile.DrivingSeconds, profile.CruisingSpeedKmh),
        new ScenarioPhase(ScenarioPhaseKind.Speeding, profile.SpeedingSeconds, profile.SpeedingSpeedKmh),
        new ScenarioPhase(ScenarioPhaseKind.Driving, profile.RecoverySeconds, profile.CruisingSpeedKmh),
        new ScenarioPhase(ScenarioPhaseKind.Stopped, profile.StopSeconds, 0),
        new ScenarioPhase(ScenarioPhaseKind.Resuming, profile.ResumeSeconds, profile.CruisingSpeedKmh)
    ]);

    public static IReadOnlyList<ScenarioPhase> PhasesFor(int vehicleIndex)
        => FromProfile(SimulationProfile.DefaultFor(vehicleIndex));
}
