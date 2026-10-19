using GpsTracking.Application.Abstractions;
using GpsTracking.Domain.Models;

namespace GpsTracking.Application.Tracking.Commands;

public sealed class UpdateVehicleSettingsHandler(ITrackingStore store, TimeProvider timeProvider, ITrackingPublisher publisher)
{
    public async Task<SettingsUpdateResult<VehicleSnapshot>> HandleAsync(string id, VehicleSettings settings)
    {
        var error = settings.Validate();
        if (error is not null)
        {
            return new SettingsUpdateResult<VehicleSnapshot>(null, error);
        }

        var snapshot = store.UpdateSettings(id, settings, timeProvider.GetUtcNow());
        if (snapshot is null)
        {
            return new SettingsUpdateResult<VehicleSnapshot>(null, NotFound: true);
        }

        await publisher.PublishAsync(snapshot);

        return new SettingsUpdateResult<VehicleSnapshot>(snapshot);
    }
}
