using GpsTracking.Application.Abstractions;
using GpsTracking.Application.Tracking;
using GpsTracking.Domain.Models;
using GpsTracking.Domain.Rules;

namespace GpsTracking.Infrastructure.Storage;

public sealed class InMemoryTrackingStore : ITrackingStore
{
    // The dictionary is constructed once. Each entry owns its mutation gate.
    private readonly Dictionary<string, VehicleSession> sessions;
    private readonly object globalGate = new();
    private GlobalSettingsSnapshot globalSettings;

    public InMemoryTrackingStore(IEnumerable<VehicleDefinition> vehicles, GlobalSettings? settings = null)
    {
        globalSettings = new GlobalSettingsSnapshot(settings ?? new GlobalSettings(), 0);
        sessions = vehicles.ToDictionary(vehicle => vehicle.Id,
            vehicle => new VehicleSession(vehicle), StringComparer.Ordinal);
    }

    public SampleAcceptance Append(GpsSample sample, DateTimeOffset receivedAt)
        => AppendWithSnapshot(sample, receivedAt).Acceptance;

    public SampleAppendResult AppendWithSnapshot(GpsSample sample, DateTimeOffset receivedAt)
    {
        if (!sessions.TryGetValue(sample.VehicleId, out var session))
        {
            return new(SampleAcceptance.UnknownVehicle);
        }

        lock (session.Gate)
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
            session.Revision++;

            return new(SampleAcceptance.Accepted, Snapshot(session));
        }
    }

    public IReadOnlyList<VehicleSnapshot> GetSnapshots()
    {
        var snapshots = new List<VehicleSnapshot>(sessions.Count);
        foreach (var session in sessions.Values)
        {
            lock (session.Gate)
            {
                snapshots.Add(Snapshot(session));
            }
        }

        return snapshots.AsReadOnly();
    }

    public VehicleSnapshot? UpdateSettings(string vehicleId, VehicleSettings settings, DateTimeOffset changedAt)
    {
        if (!sessions.TryGetValue(vehicleId, out var session))
        {
            return null;
        }

        lock (session.Gate)
        {
            if (session.Settings == settings)
            {
                return Snapshot(session);
            }

            var speedingChanged = session.Settings.SpeedLimitKmh != settings.SpeedLimitKmh
                || session.Settings.SpeedingDurationSeconds != settings.SpeedingDurationSeconds;
            session.Settings = settings;

            if (speedingChanged)
            {
                var alert = session.Speeding.Reevaluate(session.History, settings, changedAt);
                if (alert is not null)
                {
                    session.Alerts.Add(alert);
                }
            }

            session.Revision++;

            return Snapshot(session);
        }
    }

    public GlobalSettingsSnapshot GetGlobalSettings()
    {
        lock (globalGate)
        {
            return globalSettings;
        }
    }

    public GlobalSettingsSnapshot UpdateGlobalSettings(GlobalSettings settings)
    {
        lock (globalGate)
        {
            if (globalSettings.Settings != settings)
            {
                globalSettings = new GlobalSettingsSnapshot(settings, globalSettings.Revision + 1);
            }

            return globalSettings;
        }
    }

    private static VehicleSnapshot Snapshot(VehicleSession session)
        => new(session.Vehicle.Id, session.Vehicle.Name,
            session.History.Count == 0 ? null : session.History[^1],
            session.LastReceivedAt, session.History.Count, session.Revision, session.Settings,
            session.Alerts.ToArray());

    private sealed class VehicleSession(VehicleDefinition vehicle)
    {
        public object Gate { get; } = new();
        public VehicleDefinition Vehicle { get; } = vehicle;
        public List<GpsSample> History { get; } = [];
        public VehicleSettings Settings { get; set; } = vehicle.Settings;
        public SpeedingRule Speeding { get; } = new();
        public List<TrackingAlert> Alerts { get; } = [];
        public DateTimeOffset? LastReceivedAt { get; set; }
        public long Revision { get; set; }
    }
}
