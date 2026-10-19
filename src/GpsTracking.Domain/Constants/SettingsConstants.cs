namespace GpsTracking.Domain.Constants;

public static class SettingsConstants
{
    public const double DefaultSpeedLimitKmh = 80;
    public const int DefaultSpeedingDurationSeconds = 30;
    public const int DefaultStopDurationSeconds = 180;
    public const double DefaultStopRadiusMeters = 10;
    public const string InvalidVehicleSettings = "Speed limit and durations must be positive; speed limit must be finite.";
    public const string InvalidGlobalSettings = "Stop radius must be finite and positive.";
    public const string SpeedingAlertType = "Speeding";
}
