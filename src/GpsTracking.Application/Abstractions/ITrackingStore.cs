using GpsTracking.Application.Tracking;
using GpsTracking.Domain.Models;

namespace GpsTracking.Application.Abstractions;

public interface ITrackingStore
{
    SampleAcceptance Append(GpsSample sample, DateTimeOffset receivedAt);

    SampleAppendResult AppendWithSnapshot(GpsSample sample, DateTimeOffset receivedAt);

    IReadOnlyList<VehicleSnapshot> GetSnapshots();

    VehicleSnapshot? UpdateSettings(string vehicleId, VehicleSettings settings, DateTimeOffset changedAt);

    GlobalSettingsSnapshot GetGlobalSettings();

    GlobalSettingsSnapshot UpdateGlobalSettings(GlobalSettings settings);
}
