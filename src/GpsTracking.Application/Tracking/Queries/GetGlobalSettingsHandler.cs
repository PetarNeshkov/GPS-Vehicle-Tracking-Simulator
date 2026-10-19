using GpsTracking.Application.Abstractions;

namespace GpsTracking.Application.Tracking.Queries;

public sealed class GetGlobalSettingsHandler(ITrackingStore store)
{
    public GlobalSettingsSnapshot Handle()
        => store.GetGlobalSettings();
}
