using GpsTracking.Api.Constants;
using GpsTracking.Domain.Constants;
using Microsoft.Extensions.Options;

namespace GpsTracking.Api.Configuration;

public sealed class TrackingOptionsValidator : IValidateOptions<TrackingOptions>
{
    public ValidateOptionsResult Validate(string? name, TrackingOptions options)
    {
        var errors = new List<string>();
        var vehicles = options.Vehicles ?? [];
        var geofences = options.Geofences ?? [];

        var globalError = options.GlobalSettings?.Validate();
        if (options.GlobalSettings is null || globalError is not null)
        {
            errors.Add(globalError ?? SettingsConstants.InvalidGlobalSettings);
        }

        if (vehicles.Length != TrackingValidationConstants.RequiredVehicleCount)
        {
            errors.Add(TrackingValidationConstants.InvalidVehicleCount);
        }

        var vehicleIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var vehicle in vehicles)
        {
            if (vehicle is null || string.IsNullOrWhiteSpace(vehicle.Id)
                || string.IsNullOrWhiteSpace(vehicle.Name) || !vehicleIds.Add(vehicle.Id))
            {
                errors.Add(TrackingValidationConstants.InvalidVehicleIdentity);
                continue;
            }

            var settings = vehicle.Settings;
            if (settings is null || settings.Validate() is not null)
            {
                errors.Add(string.Format(TrackingValidationConstants.InvalidVehicleSettingsFormat, vehicle.Id));
            }
        }

        if (geofences.Length < TrackingValidationConstants.MinimumGeofenceCount)
            errors.Add(TrackingValidationConstants.MissingGeofences);

        var geofenceIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var geofence in geofences)
        {
            if (geofence is null || string.IsNullOrWhiteSpace(geofence.Id)
                || string.IsNullOrWhiteSpace(geofence.Name) || !geofenceIds.Add(geofence.Id))
            {
                errors.Add(TrackingValidationConstants.InvalidGeofenceIdentity);
                continue;
            }

            if (!ValidCoordinate(geofence.MinLatitude, GpsSampleConstants.MaximumLatitude)
                || !ValidCoordinate(geofence.MaxLatitude, GpsSampleConstants.MaximumLatitude)
                || !ValidCoordinate(geofence.MinLongitude, GpsSampleConstants.MaximumLongitude)
                || !ValidCoordinate(geofence.MaxLongitude, GpsSampleConstants.MaximumLongitude)
                || geofence.MinLatitude >= geofence.MaxLatitude || geofence.MinLongitude >= geofence.MaxLongitude)
            {
                errors.Add(string.Format(TrackingValidationConstants.InvalidGeofenceCoordinatesFormat, geofence.Id));
            }
        }

        return errors.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(errors);
    }

    private static bool ValidCoordinate(double value, double limit)
        => double.IsFinite(value) && value >= -limit && value <= limit;
}
