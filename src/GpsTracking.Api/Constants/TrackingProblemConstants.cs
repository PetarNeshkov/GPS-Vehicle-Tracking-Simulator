namespace GpsTracking.Api.Constants;

public static class TrackingProblemConstants
{
    public const string InvalidSettingsTitle = "Invalid tracking settings";
    public const string InvalidSampleTitle = "Invalid GPS sample";
    public const string UnknownVehicleTitle = "Unknown vehicle";
    public const string UnknownVehicleDetailFormat = "Vehicle '{0}' is not configured.";
    public const string TimestampNotNewerTitle = "Timestamp is not newer";
    public const string TimestampNotNewerDetail = "Samples must have strictly increasing generation timestamps.";
    public const string UnexpectedAcceptance = "Unexpected sample acceptance result.";
}
