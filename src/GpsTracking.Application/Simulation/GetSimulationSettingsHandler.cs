using GpsTracking.Application.Abstractions;

namespace GpsTracking.Application.Simulation;

public sealed class GetSimulationSettingsHandler(ISimulationSettingsStore store)
{
    public SimulationSettings? Handle(string vehicleId)
        => store.Get(vehicleId);
}
