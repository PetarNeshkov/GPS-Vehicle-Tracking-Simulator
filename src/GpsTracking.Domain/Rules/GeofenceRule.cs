using GpsTracking.Domain.Constants;
using GpsTracking.Domain.Models;

namespace GpsTracking.Domain.Rules;

public sealed class GeofenceRule(GeofenceDefinition definition)
{
    private readonly Queue<bool> observations = new();
    private GeofenceState state = GeofenceState.Unknown;

    public GeofenceMembership Membership => new(definition.Id, state);

    public TrackingAlert? Observe(GpsSample sample, DateTimeOffset createdAt)
    {
        var inside = sample.Latitude >= definition.MinLatitude
            && sample.Latitude <= definition.MaxLatitude
            && sample.Longitude >= definition.MinLongitude
            && sample.Longitude <= definition.MaxLongitude;

        observations.Enqueue(inside);
        if (observations.Count > GeofenceConstants.ObservationWindowSize)
        {
            observations.Dequeue();
        }

        if (observations.Count < GeofenceConstants.ObservationWindowSize)
        {
            return null;
        }

        var next = observations.Count(value => value) >= GeofenceConstants.RequiredMajority
            ? GeofenceState.Inside : GeofenceState.Outside;
        var previous = state;
        state = next;

        if (previous == GeofenceState.Unknown || previous == next)
        {
            return null;
        }

        var type = next == GeofenceState.Inside
            ? GeofenceConstants.EnteredAlertType : GeofenceConstants.ExitedAlertType;

        return new TrackingAlert(Guid.NewGuid(), sample.VehicleId, type, sample.Timestamp,
            createdAt, null, null, null, definition.Id, definition.Name);
    }
}
