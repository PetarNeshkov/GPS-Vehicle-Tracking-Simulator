namespace GpsTracking.Contracts;

public static class ApiRoutes
{
    public const string VehiclesPath = "api/vehicles";
    public const string VehicleSamples = "{id}/samples";
    public const string VehicleSettings = "{id}/settings";

    public static string SamplesFor(string vehicleId)
        => $"{VehiclesPath}/{Uri.EscapeDataString(vehicleId)}/samples";
}
