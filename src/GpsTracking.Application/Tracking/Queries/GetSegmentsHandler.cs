using GpsTracking.Application.Abstractions;

namespace GpsTracking.Application.Tracking.Queries;

public sealed class GetSegmentsHandler(ITrackingStore store)
{
    public VehicleSnapshot? Handle(string id)
        => store.GetSnapshots().FirstOrDefault(vehicle => vehicle.Id == id);
}
