namespace GpsTracking.Contracts;

public static class ApiRoutes
{
    public const string VehiclesPath = "api/vehicles";
    public const string VehicleSamples = "{id}/samples";
    public const string VehicleHistory = "{id}/history";
    public const string VehicleSegments = "{id}/segments";
    public const string VehicleSettings = "{id}/settings";
    public const string VehicleSimulation = "{id}/simulation";

    public static string SimulationFor(string vehicleId)
        => $"{VehiclesPath}/{Uri.EscapeDataString(vehicleId)}/simulation";

    public static string SamplesFor(string vehicleId)
        => $"{VehiclesPath}/{Uri.EscapeDataString(vehicleId)}/samples";
}
