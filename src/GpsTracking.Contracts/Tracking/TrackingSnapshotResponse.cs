namespace GpsTracking.Contracts.Tracking;

public sealed record TrackingSnapshotResponse(
    Guid SessionId,
    IReadOnlyList<VehicleSnapshotResponse> Vehicles,
    GlobalSettingsResponse? GlobalSettings = null);

public sealed record VehicleUpdatedEvent(Guid SessionId, VehicleSnapshotResponse Vehicle);
