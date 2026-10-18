namespace GpsTracking.Application.Tracking;

public sealed record SubmitSampleCommand(
    string VehicleId,
    DateTimeOffset Timestamp,
    double Latitude,
    double Longitude,
    double SpeedKmh,
    double Heading);
