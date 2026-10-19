using GpsTracking.Application.Abstractions;
using GpsTracking.Domain.Models;

namespace GpsTracking.Application.Tracking.Queries;

public sealed class GetAlertsHandler(ITrackingStore store)
{
    public IReadOnlyList<TrackingAlert> Handle()
        => store.GetSnapshots().SelectMany(vehicle => vehicle.Alerts ?? [])
            .OrderByDescending(alert => alert.OccurredAt)
            .ThenBy(alert => alert.Id)
            .ToArray();
}
