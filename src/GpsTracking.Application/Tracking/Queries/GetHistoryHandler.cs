using GpsTracking.Application.Abstractions;

namespace GpsTracking.Application.Tracking.Queries;

public sealed class GetHistoryHandler(ITrackingStore store)
{
    public HistorySnapshot? Handle(string id)
        => store.GetHistory(id);
}
