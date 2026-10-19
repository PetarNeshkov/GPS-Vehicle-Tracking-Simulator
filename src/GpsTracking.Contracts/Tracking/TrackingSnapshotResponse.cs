namespace GpsTracking.Contracts.Tracking;

public sealed record TrackingSnapshotResponse(Guid SessionId, IReadOnlyList<VehicleSnapshotResponse> Vehicles);

public sealed record VehicleUpdatedEvent(Guid SessionId, VehicleSnapshotResponse Vehicle);
