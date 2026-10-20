namespace GpsTracking.Simulator.Configuration;

public static class SimulatorConstants
{
    public const string ProfileUnavailable = "{VehicleId}: simulation profile unavailable; continuing the current scenario.";
    public const int MinimumTickSeconds = 10;
    public const int VehicleCount = 2;
    public const int DeliveryTimeoutSeconds = 5;
    public const string InvalidBackendUrl = "Simulator:BackendBaseUrl must be an absolute HTTP(S) URL.";
    public const string InvalidTick = "Simulator:TickIntervalSeconds must be at least 10.";
    public const string InvalidVehicles = "Simulator:VehicleIds must contain exactly two distinct non-empty identifiers.";
    public const string InvalidPhases = "A scenario requires named phases with positive durations, finite nonnegative speeds, and at least one moving phase. Stopped phases must have zero speed; other phases must move.";
    public const string InsufficientDemoCoverage = "Demo phases must cover the default speeding and stop durations with two reporting intervals of margin. Reduce Simulator:TickIntervalSeconds or lengthen the phases.";
    public const string InvalidRoute = "A route needs at least two valid points.";
    public const string DuplicateRoutePoints = "Adjacent route points must be distinct, including the closing edge.";
    public const string PhaseChanged = "{VehicleId}: {Phase}, speed {SpeedKmh} km/h";
    public const string DeliveryFailed = "Delivery failed for {VehicleId} at {GpsTimestamp}; continuing next interval";
}
