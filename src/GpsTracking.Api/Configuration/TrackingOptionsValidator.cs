using Microsoft.Extensions.Options;

namespace GpsTracking.Api.Configuration;

public sealed class TrackingOptionsValidator : IValidateOptions<TrackingOptions>
{
    public ValidateOptionsResult Validate(string? name, TrackingOptions options)
    {
        var errors = new List<string>();
        var vehicles = options.Vehicles ?? [];
        var geofences = options.Geofences ?? [];

        if (vehicles.Length != 2)
            errors.Add("Tracking:Vehicles must contain exactly two vehicles.");

        var vehicleIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var vehicle in vehicles)
        {
            if (vehicle is null || string.IsNullOrWhiteSpace(vehicle.Id)
                || string.IsNullOrWhiteSpace(vehicle.Name) || !vehicleIds.Add(vehicle.Id))
            {
                errors.Add("Each vehicle must have a unique non-empty Id and a non-empty Name.");
                continue;
            }

            var settings = vehicle.Settings;
            if (settings is null || !double.IsFinite(settings.SpeedLimitKmh) || settings.SpeedLimitKmh <= 0
                || settings.SpeedingDurationSeconds <= 0 || settings.StopDurationSeconds <= 0
                || !double.IsFinite(settings.StopRadiusMeters) || settings.StopRadiusMeters <= 0)
                errors.Add($"Vehicle '{vehicle.Id}' must have finite positive thresholds, durations and radius.");
        }

        if (geofences.Length == 0)
            errors.Add("Tracking:Geofences must contain at least one rectangular region.");

        var geofenceIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var geofence in geofences)
        {
            if (geofence is null || string.IsNullOrWhiteSpace(geofence.Id)
                || string.IsNullOrWhiteSpace(geofence.Name) || !geofenceIds.Add(geofence.Id))
            {
                errors.Add("Each geofence must have a unique non-empty Id and a non-empty Name.");
                continue;
            }

            if (!ValidCoordinate(geofence.MinLatitude, 90) || !ValidCoordinate(geofence.MaxLatitude, 90)
                || !ValidCoordinate(geofence.MinLongitude, 180) || !ValidCoordinate(geofence.MaxLongitude, 180)
                || geofence.MinLatitude >= geofence.MaxLatitude || geofence.MinLongitude >= geofence.MaxLongitude)
                errors.Add($"Geofence '{geofence.Id}' must have valid coordinates with minimums below maximums.");
        }

        return errors.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(errors);
    }

    private static bool ValidCoordinate(double value, double limit) =>
        double.IsFinite(value) && value >= -limit && value <= limit;
}
