using GpsTracking.Application.Abstractions;
using GpsTracking.Application.Tracking;
using GpsTracking.Domain.Models;

namespace GpsTracking.Infrastructure.Storage;

public sealed class InMemoryTrackingStore : ITrackingStore
{
    // The dictionary is constructed once. Each entry owns its mutation gate.
    private readonly Dictionary<string, VehicleSession> sessions;

    public InMemoryTrackingStore(IEnumerable<VehicleDefinition> vehicles)
    {
        sessions = vehicles.ToDictionary(vehicle => vehicle.Id,
            vehicle => new VehicleSession(vehicle), StringComparer.Ordinal);
    }

    public SampleAcceptance Append(GpsSample sample, DateTimeOffset receivedAt)
    {
        if (!sessions.TryGetValue(sample.VehicleId, out var session))
            return SampleAcceptance.UnknownVehicle;

        lock (session.Gate)
        {
            if (session.History.Count > 0 && sample.Timestamp <= session.History[^1].Timestamp)
                return SampleAcceptance.TimestampNotNewer;

            session.History.Add(sample);
            session.LastReceivedAt = receivedAt.ToUniversalTime();
            session.Revision++;
            return SampleAcceptance.Accepted;
        }
    }

    public IReadOnlyList<VehicleSnapshot> GetSnapshots()
    {
        var snapshots = new List<VehicleSnapshot>(sessions.Count);
        foreach (var session in sessions.Values)
        {
            lock (session.Gate)
            {
                snapshots.Add(new VehicleSnapshot(session.Vehicle.Id, session.Vehicle.Name,
                    session.History.Count == 0 ? null : session.History[^1],
                    session.LastReceivedAt, session.History.Count, session.Revision));
            }
        }

        return snapshots.AsReadOnly();
    }

    private sealed class VehicleSession(VehicleDefinition vehicle)
    {
        public object Gate { get; } = new();
        public VehicleDefinition Vehicle { get; } = vehicle;
        public List<GpsSample> History { get; } = [];
        public DateTimeOffset? LastReceivedAt { get; set; }
        public long Revision { get; set; }
    }
}
