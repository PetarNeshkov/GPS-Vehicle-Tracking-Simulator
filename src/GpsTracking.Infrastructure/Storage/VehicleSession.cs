using GpsTracking.Domain.Models;
using GpsTracking.Domain.Rules;

namespace GpsTracking.Infrastructure.Storage;

public sealed class VehicleSession(VehicleDefinition vehicle, IReadOnlyList<GeofenceDefinition> geofences)
{
    public IReadOnlyList<GeofenceRule> Geofences { get; } = geofences.Select(definition => new GeofenceRule(definition)).ToArray();

    public MovementRule Movement { get; } = new();

    public VehicleDefinition Vehicle { get; } = vehicle;

    public List<GpsSample> History { get; } = [];

    public VehicleSettings Settings { get; set; } = vehicle.Settings;

    public SpeedingRule Speeding { get; } = new();

    public List<TrackingAlert> Alerts { get; } = [];

    public DateTimeOffset? LastReceivedAt { get; set; }

    public long Revision { get; set; }
}
