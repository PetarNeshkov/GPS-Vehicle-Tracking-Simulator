namespace GpsTracking.Contracts.Tracking;

public sealed record MovementResponse(string State, DateTimeOffset? StopStartedAt, double StopElapsedSeconds);

public sealed record RouteSegmentResponse(Guid Id,
    DateTimeOffset StartedAt,
    DateTimeOffset? EndedAt,
    IReadOnlyList<GpsSampleResponse> Points);

public sealed record HistoryResponse(Guid SessionId, string VehicleId, long Revision, IReadOnlyList<GpsSampleResponse> Samples);

public sealed record SegmentsResponse(Guid SessionId, string VehicleId, long Revision, IReadOnlyList<RouteSegmentResponse> Segments);
