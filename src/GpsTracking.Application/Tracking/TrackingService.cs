using GpsTracking.Application.Abstractions;
using GpsTracking.Domain.Models;

namespace GpsTracking.Application.Tracking;

public sealed class TrackingService(ITrackingStore store, TimeProvider timeProvider, ITrackingPublisher publisher)
{
    public async Task<SubmitSampleResult> SubmitSampleAsync(SubmitSampleCommand command)
    {
        GpsSample sample;
        try
        {
            sample = new GpsSample(command.VehicleId, command.Timestamp, command.Latitude,
                command.Longitude, command.SpeedKmh, command.Heading);
        }
        catch (ArgumentException exception)
        {
            return new SubmitSampleResult(SampleAcceptance.InvalidSample, exception.Message);
        }

        var result = store.AppendWithSnapshot(sample, timeProvider.GetUtcNow());
        if (result.Snapshot is { } snapshot)
        {
            await publisher.PublishAsync(snapshot);
        }

        return new SubmitSampleResult(result.Acceptance);
    }

    public IReadOnlyList<VehicleSnapshot> GetSnapshots()
        => store.GetSnapshots();

    public VehicleSnapshot? GetVehicle(string id)
        => store.GetSnapshots().FirstOrDefault(vehicle => vehicle.Id == id);

    public IReadOnlyList<TrackingAlert> GetAlerts()
        => store.GetSnapshots().SelectMany(vehicle => vehicle.Alerts ?? [])
            .OrderByDescending(alert => alert.OccurredAt)
            .ThenBy(alert => alert.Id)
            .ToArray();

    public GlobalSettingsSnapshot GetGlobalSettings() => store.GetGlobalSettings();

    public async Task<SettingsUpdateResult<VehicleSnapshot>> UpdateSettingsAsync(string id, VehicleSettings settings)
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

    public async Task<SettingsUpdateResult<GlobalSettingsSnapshot>> UpdateGlobalSettingsAsync(GlobalSettings settings)
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
