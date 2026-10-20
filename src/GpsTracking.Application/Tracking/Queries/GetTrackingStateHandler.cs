using GpsTracking.Application.Abstractions;

namespace GpsTracking.Application.Tracking.Queries;

public sealed class GetTrackingStateHandler(ITrackingStore store)
{
    public TrackingState Handle()
        => store.GetState();
}
