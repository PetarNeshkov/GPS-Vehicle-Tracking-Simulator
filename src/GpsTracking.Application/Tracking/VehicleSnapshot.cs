using GpsTracking.Domain.Models;

namespace GpsTracking.Application.Tracking;

public sealed record VehicleSnapshot(
    string Id,
    string Name,
    GpsSample? LatestSample,
    DateTimeOffset? LastReceivedAt,
    int SampleCount,
    long Revision,
    VehicleSettings? Settings = null,
    IReadOnlyList<TrackingAlert>? Alerts = null,
    IReadOnlyList<GeofenceMembership>? GeofenceMemberships = null,
    MovementSnapshot? Movement = null,
    IReadOnlyList<RouteSegment>? Segments = null);
