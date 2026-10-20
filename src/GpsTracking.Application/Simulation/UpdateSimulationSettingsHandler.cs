using GpsTracking.Application.Abstractions;

namespace GpsTracking.Application.Simulation;

public enum SimulationUpdateStatus { Updated, Invalid, NotFound }

public sealed class UpdateSimulationSettingsHandler(ISimulationSettingsStore store)
{
    public SimulationUpdateStatus Handle(string vehicleId, SimulationSettings settings)
    {
        if (!settings.IsValid)
        {
            return SimulationUpdateStatus.Invalid;
        }

        return store.Update(vehicleId, settings)
            ? SimulationUpdateStatus.Updated
            : SimulationUpdateStatus.NotFound;
    }
}
