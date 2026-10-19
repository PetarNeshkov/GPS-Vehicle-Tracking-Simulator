namespace GpsTracking.Contracts.Tracking;

public sealed record GeofenceResponse(
    string Id,
    string Name,
    double MinLatitude,
    double MaxLatitude,
    double MinLongitude,
    double MaxLongitude);

public sealed record GeofencesResponse(Guid SessionId, IReadOnlyList<GeofenceResponse> Geofences);

public sealed record GeofenceMembershipResponse(string GeofenceId, string State);
