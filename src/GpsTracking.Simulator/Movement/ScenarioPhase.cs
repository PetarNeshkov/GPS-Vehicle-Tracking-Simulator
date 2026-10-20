namespace GpsTracking.Simulator.Movement;

public enum ScenarioPhaseKind
{
    Driving,
    Speeding,
    Stopped,
    Resuming
}

public sealed record ScenarioPhase(ScenarioPhaseKind Kind, int DurationSeconds, double SpeedKmh);

public sealed record ScenarioPosition(RoutePosition Position, ScenarioPhaseKind Phase);
