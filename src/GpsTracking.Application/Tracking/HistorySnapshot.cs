using GpsTracking.Domain.Models;

namespace GpsTracking.Application.Tracking;

public sealed record HistorySnapshot(string VehicleId, long Revision, IReadOnlyList<GpsSample> Samples);

public sealed record TrackingState(IReadOnlyList<VehicleSnapshot> Vehicles, GlobalSettingsSnapshot GlobalSettings);
