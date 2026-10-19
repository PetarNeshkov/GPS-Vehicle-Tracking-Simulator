using GpsTracking.Application.Abstractions;

namespace GpsTracking.Application.Tracking.Queries;

public sealed class GetSnapshotsHandler(ITrackingStore store)
{
    public IReadOnlyList<VehicleSnapshot> Handle()
        => store.GetSnapshots();
}
