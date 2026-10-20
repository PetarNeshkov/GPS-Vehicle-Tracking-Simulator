namespace GpsTracking.Domain.Models;

public enum MovementState { Unknown, Moving, PossibleStop, Stopped }

public sealed record MovementSnapshot(MovementState State, DateTimeOffset? StopStartedAt,
    double StopElapsedSeconds);

public sealed record RouteSegment(Guid Id, DateTimeOffset StartedAt, DateTimeOffset? EndedAt,
    IReadOnlyList<GpsSample> Points);
