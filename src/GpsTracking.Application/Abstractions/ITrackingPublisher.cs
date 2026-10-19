using GpsTracking.Application.Tracking;

namespace GpsTracking.Application.Abstractions;

public interface ITrackingPublisher
{
    Task PublishAsync(VehicleSnapshot snapshot);

    Task PublishGlobalSettingsAsync(GlobalSettingsSnapshot snapshot);
}
