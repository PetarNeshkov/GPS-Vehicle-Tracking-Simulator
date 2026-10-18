using GpsTracking.Application.Tracking;
using GpsTracking.Domain.Models;

namespace GpsTracking.Application.Abstractions;

public interface ITrackingStore
{
    SampleAcceptance Append(GpsSample sample, DateTimeOffset receivedAt);

    IReadOnlyList<VehicleSnapshot> GetSnapshots();
}
