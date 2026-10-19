using GpsTracking.Application.Abstractions;
using GpsTracking.Domain.Models;

namespace GpsTracking.Application.Tracking.Queries;

public sealed class GetGeofencesHandler(ITrackingStore store)
{
    public IReadOnlyList<GeofenceDefinition> Handle()
        => store.GetGeofences();
}
