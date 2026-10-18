namespace GpsTracking.Contracts.Tracking;

public sealed record GpsSampleResponse(
    DateTimeOffset Timestamp,
    double Latitude,
    double Longitude,
    double SpeedKmh,
    double Heading);

public sealed record VehicleSnapshotResponse(
    string Id,
    string Name,
    GpsSampleResponse? LatestSample,
    DateTimeOffset? LastReceivedAt,
    int SampleCount,
    long Revision);
