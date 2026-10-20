using GpsTracking.Application.Abstractions;
using GpsTracking.Application.Tracking;
using GpsTracking.Domain.Models;
using GpsTracking.Domain.Rules;

namespace GpsTracking.Infrastructure.Storage;

public sealed class InMemoryTrackingStore : ITrackingStore
{
    // One gate keeps global radius changes atomic with both vehicles and reads.
    private readonly Dictionary<string, VehicleSession> sessions;
    private readonly IReadOnlyList<GeofenceDefinition> geofences;
    private readonly object gate = new();
    private GlobalSettingsSnapshot globalSettings;

    public InMemoryTrackingStore(IEnumerable<VehicleDefinition> vehicles, GlobalSettings? settings = null,
        IEnumerable<GeofenceDefinition>? geofenceDefinitions = null)
    {
        geofences = Array.AsReadOnly((geofenceDefinitions ?? []).ToArray());
        globalSettings = new GlobalSettingsSnapshot(settings ?? new GlobalSettings(), 0);
        sessions = vehicles.ToDictionary(vehicle => vehicle.Id,
            vehicle => new VehicleSession(vehicle, geofences), StringComparer.Ordinal);
    }

    public SampleAcceptance Append(GpsSample sample, DateTimeOffset receivedAt)
        => AppendWithSnapshot(sample, receivedAt).Acceptance;

    public SampleAppendResult AppendWithSnapshot(GpsSample sample, DateTimeOffset receivedAt)
    {
        if (!sessions.TryGetValue(sample.VehicleId, out var session))
        {
            return new(SampleAcceptance.UnknownVehicle);
        }

        lock (gate)
        {
            if (session.History.Count > 0 && sample.Timestamp <= session.History[^1].Timestamp)
            {
                return new(SampleAcceptance.TimestampNotNewer);
            }

            session.History.Add(sample);
            session.LastReceivedAt = receivedAt.ToUniversalTime();
            var alert = session.Speeding.Observe(sample, session.Settings, receivedAt);
            if (alert is not null)
            {
                session.Alerts.Add(alert);
            }

            foreach (var rule in session.Geofences)
            {
                var geofenceAlert = rule.Observe(sample, receivedAt);
                if (geofenceAlert is not null)
                {
                    session.Alerts.Add(geofenceAlert);
                }
            }

            session.Movement.Observe(sample, globalSettings.Settings.StopRadiusMeters, session.Settings.StopDurationSeconds);
            session.Revision++;

            return new(SampleAcceptance.Accepted, Snapshot(session));
        }
    }

    public IReadOnlyList<GeofenceDefinition> GetGeofences()
        => geofences;

    public IReadOnlyList<VehicleSnapshot> GetSnapshots()
    {
        lock (gate) return sessions.Values.Select(Snapshot).ToArray();
    }

    public TrackingState GetState()
    {
        lock (gate)
        {
            var vehicleSnapshots = sessions.Values
                .Select(Snapshot)
                .ToArray();

            return new TrackingState(vehicleSnapshots, globalSettings);
        }
    }

    public HistorySnapshot? GetHistory(string vehicleId)
    {
        lock (gate)
        {
            return sessions.TryGetValue(vehicleId, out var session)
                ? new HistorySnapshot(vehicleId, session.Revision, session.History.ToArray())
                : null;
        }
    }

    public VehicleSnapshot? UpdateSettings(string vehicleId, VehicleSettings settings, DateTimeOffset changedAt)
    {
        if (!sessions.TryGetValue(vehicleId, out var session))
        {
            return null;
        }

        lock (gate)
        {
            if (session.Settings == settings)
            {
                return Snapshot(session);
            }

            var speedingChanged = session.Settings.SpeedLimitKmh != settings.SpeedLimitKmh
                || session.Settings.SpeedingDurationSeconds != settings.SpeedingDurationSeconds;
            var stopChanged = session.Settings.StopDurationSeconds != settings.StopDurationSeconds;
            session.Settings = settings;

            if (speedingChanged)
            {
                var alert = session.Speeding.Reevaluate(session.History, settings, changedAt);
                if (alert is not null)
                {
                    session.Alerts.Add(alert);
                }
            }

            if (stopChanged)
            {
                session.Movement.Reevaluate(globalSettings.Settings.StopRadiusMeters, settings.StopDurationSeconds);
            }

            session.Revision++;

            return Snapshot(session);
        }
    }

    public GlobalSettingsSnapshot GetGlobalSettings()
    {
        lock (gate)
        {
            return globalSettings;
        }
    }

    public GlobalSettingsSnapshot UpdateGlobalSettings(GlobalSettings settings)
    {
        lock (gate)
        {
            if (globalSettings.Settings != settings)
            {
                globalSettings = new GlobalSettingsSnapshot(settings, globalSettings.Revision + 1);
                foreach (var session in sessions.Values)
                {
                    session.Movement.Reevaluate(settings.StopRadiusMeters, session.Settings.StopDurationSeconds);
                    session.Revision++;
                }
            }

            return globalSettings with { Vehicles = sessions.Values.Select(Snapshot).ToArray() };
        }
    }

    private static VehicleSnapshot Snapshot(VehicleSession session)
        => new(session.Vehicle.Id, session.Vehicle.Name,
            session.History.Count == 0 ? null : session.History[^1],
            session.LastReceivedAt, session.History.Count, session.Revision, session.Settings,
            session.Alerts.ToArray(), session.Geofences.Select(rule => rule.Membership).ToArray(),
            session.Movement.Movement, session.Movement.Segments);
}
