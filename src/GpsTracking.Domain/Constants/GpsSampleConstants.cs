namespace GpsTracking.Domain.Constants;

public static class GpsSampleConstants
{
    public const double MinimumLatitude = -90;
    public const double MaximumLatitude = 90;
    public const double MinimumLongitude = -180;
    public const double MaximumLongitude = 180;
    public const double MinimumSpeedKmh = 0;
    public const double MinimumHeading = 0;
    public const double MaximumHeading = 360;

    public const string VehicleIdRequired = "Vehicle ID is required.";
    public const string TimestampRequired = "A generation timestamp is required.";
    public const string InvalidLatitude = "Latitude must be between -90 and 90.";
    public const string InvalidLongitude = "Longitude must be between -180 and 180.";
    public const string InvalidSpeed = "Speed must be finite and nonnegative.";
    public const string InvalidHeading = "Heading must be between 0 and 360.";
}
