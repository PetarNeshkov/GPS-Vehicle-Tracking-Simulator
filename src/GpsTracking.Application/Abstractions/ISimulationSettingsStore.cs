using GpsTracking.Application.Simulation;

namespace GpsTracking.Application.Abstractions;

public interface ISimulationSettingsStore
{
    SimulationSettings? Get(string vehicleId);

    bool Update(string vehicleId, SimulationSettings settings);
}
