namespace GpsTracking.Api.Constants;

public static class TrackingValidationConstants
{
    public const int RequiredVehicleCount = 2;
    public const int MinimumGeofenceCount = 1;
    public const double PositiveThresholdLowerBound = 0;

    public const string InvalidVehicleCount = "Tracking:Vehicles must contain exactly two vehicles.";
    public const string InvalidVehicleIdentity = "Each vehicle must have a unique non-empty Id and a non-empty Name.";
    public const string InvalidVehicleSettingsFormat = "Vehicle '{0}' must have finite positive thresholds, durations and radius.";
    public const string MissingGeofences = "Tracking:Geofences must contain at least one rectangular region.";
    public const string InvalidGeofenceIdentity = "Each geofence must have a unique non-empty Id and a non-empty Name.";
    public const string InvalidGeofenceCoordinatesFormat = "Geofence '{0}' must have valid coordinates with minimums below maximums.";
}
