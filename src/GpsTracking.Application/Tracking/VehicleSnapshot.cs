using GpsTracking.Domain.Models;

namespace GpsTracking.Application.Tracking;

public sealed record VehicleSnapshot(
    string Id,
    string Name,
    GpsSample? LatestSample,
    DateTimeOffset? LastReceivedAt,
    int SampleCount,
    long Revision);
