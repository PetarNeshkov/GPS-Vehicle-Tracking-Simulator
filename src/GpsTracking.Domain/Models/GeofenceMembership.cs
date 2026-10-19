namespace GpsTracking.Domain.Models;

public enum GeofenceState
{
    Unknown,
    Inside,
    Outside
}

public sealed record GeofenceMembership(string GeofenceId, GeofenceState State);
