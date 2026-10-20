namespace GpsTracking.Simulator.Configuration;

public sealed class SimulatorOptions
{
    public const string SectionName = "Simulator";

    public string BackendBaseUrl { get; init; } = string.Empty;
    public int TickIntervalSeconds { get; init; } = SimulatorConstants.MinimumTickSeconds;
    public string[] VehicleIds { get; init; } = [];
}
