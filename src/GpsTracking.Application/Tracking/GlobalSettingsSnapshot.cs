using GpsTracking.Domain.Models;

namespace GpsTracking.Application.Tracking;

public sealed record GlobalSettingsSnapshot(GlobalSettings Settings, long Revision);

public sealed record SettingsUpdateResult<T>(T? Value, string? Error = null, bool NotFound = false)
    where T : class;
