using GpsTracking.Application.Abstractions;
using GpsTracking.Domain.Models;

namespace GpsTracking.Application.Tracking.Commands;

public sealed class UpdateGlobalSettingsHandler(ITrackingStore store, ITrackingPublisher publisher)
{
    public async Task<SettingsUpdateResult<GlobalSettingsSnapshot>> HandleAsync(GlobalSettings settings)
    {
        var error = settings.Validate();
        if (error is not null)
        {
            return new SettingsUpdateResult<GlobalSettingsSnapshot>(null, error);
        }

        var snapshot = store.UpdateGlobalSettings(settings);
        await publisher.PublishGlobalSettingsAsync(snapshot);

        return new SettingsUpdateResult<GlobalSettingsSnapshot>(snapshot);
    }
}
