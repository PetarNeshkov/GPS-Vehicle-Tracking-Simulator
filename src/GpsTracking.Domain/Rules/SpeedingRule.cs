using GpsTracking.Domain.Constants;
using GpsTracking.Domain.Models;

namespace GpsTracking.Domain.Rules;

public sealed class SpeedingRule
{
    private DateTimeOffset? speedingSince;
    private bool alerted;

    public TrackingAlert? Observe(GpsSample sample, VehicleSettings settings, DateTimeOffset createdAt)
    {
        if (sample.SpeedKmh <= settings.SpeedLimitKmh)
        {
            speedingSince = null;
            alerted = false;

            return null;
        }

        speedingSince ??= sample.Timestamp;

        return CreateAlertIfQualified(sample, settings, createdAt);
    }

    public TrackingAlert? Reevaluate(IReadOnlyList<GpsSample> history, VehicleSettings settings,
        DateTimeOffset createdAt)
    {
        speedingSince = null;
        if (history.Count == 0)
        {
            alerted = false;

            return null;
        }

        for (var index = history.Count - 1; index >= 0; index--)
        {
            var sample = history[index];
            if (sample.SpeedKmh <= settings.SpeedLimitKmh)
            {
                break;
            }

            speedingSince = sample.Timestamp;
        }

        var latest = history[^1];
        if (!Qualifies(latest, settings))
        {
            alerted = false;
        }

        return CreateAlertIfQualified(latest, settings, createdAt);
    }

    private bool Qualifies(GpsSample latest, VehicleSettings settings)
        => speedingSince is { } start
            && latest.Timestamp - start >= TimeSpan.FromSeconds(settings.SpeedingDurationSeconds);

    private TrackingAlert? CreateAlertIfQualified(GpsSample latest, VehicleSettings settings,
        DateTimeOffset createdAt)
    {
        if (alerted || !Qualifies(latest, settings))
        {
            return null;
        }

        alerted = true;

        return new TrackingAlert(Guid.NewGuid(),
            latest.VehicleId,
            SettingsConstants.SpeedingAlertType,
            latest.Timestamp,
            createdAt, latest.SpeedKmh,
            settings.SpeedLimitKmh,
            settings.SpeedingDurationSeconds);
    }
}
