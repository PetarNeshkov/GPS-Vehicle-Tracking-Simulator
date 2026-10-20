using GpsTracking.Application.Abstractions;
using GpsTracking.Application.Simulation;

namespace GpsTracking.Infrastructure.Storage;

public sealed class InMemorySimulationSettingsStore(IReadOnlyDictionary<string, SimulationSettings> seeds)
    : ISimulationSettingsStore
{
    private readonly Lock gate = new();
    private readonly Dictionary<string, SimulationSettings> settings = new(seeds, StringComparer.Ordinal);

    public SimulationSettings? Get(string vehicleId)
    {
        lock (gate)
        {
            return settings.GetValueOrDefault(vehicleId);
        }
    }

    public bool Update(string vehicleId, SimulationSettings value)
    {
        lock (gate)
        {
            if (!settings.ContainsKey(vehicleId))
            {
                return false;
            }

            settings[vehicleId] = value;
            return true;
        }
    }
}
