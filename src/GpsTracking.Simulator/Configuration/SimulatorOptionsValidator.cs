using Microsoft.Extensions.Options;
using GpsTracking.Simulator.Movement;

namespace GpsTracking.Simulator.Configuration;

public sealed class SimulatorOptionsValidator : IValidateOptions<SimulatorOptions>
{
    public ValidateOptionsResult Validate(string? name, SimulatorOptions options)
    {
        var errors = new List<string>();
        ValidateBackendUrl(options, errors);

        ValidateTickInterval(options, errors);

        ValidateVehicleIds(options, errors);

        ValidateScenarios(options, errors);

        return errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(errors);
    }

    private static void ValidateBackendUrl(SimulatorOptions options, List<string> errors)
    {
        if (!Uri.TryCreate(options.BackendBaseUrl, UriKind.Absolute, out var backend)
            || (backend.Scheme != Uri.UriSchemeHttp && backend.Scheme != Uri.UriSchemeHttps))
        {
            errors.Add(SimulatorConstants.InvalidBackendUrl);
        }
    }

    private static void ValidateTickInterval(SimulatorOptions options, List<string> errors)
    {
        if (options.TickIntervalSeconds < SimulatorConstants.MinimumTickSeconds)
        {
            errors.Add(SimulatorConstants.InvalidTick);
        }
    }

    private static void ValidateVehicleIds(SimulatorOptions options, List<string> errors)
    {
        var vehicleIds = options.VehicleIds ?? [];
        if (vehicleIds.Length != SimulatorConstants.VehicleCount || vehicleIds.Any(string.IsNullOrWhiteSpace)
                                                                 || vehicleIds.Distinct(StringComparer.Ordinal).Count() != SimulatorConstants.VehicleCount)
        {
            errors.Add(SimulatorConstants.InvalidVehicles);
        }
    }

    private static void ValidateScenarios(SimulatorOptions options, List<string> errors)
    {
        for (var index = 0; index < SimulatorConstants.VehicleCount; index++)
        {
            var phases = DemoScenarios.PhasesFor(index);
            var phaseError = ScenarioProgressor.Validate(phases);
            if (phaseError is not null)
            {
                errors.Add(phaseError);
                continue;
            }

            var marginSeconds = (long)options.TickIntervalSeconds * DemoScenarios.CoverageMarginIntervals;
            var speedingCoverage = phases.Any(phase => phase.SpeedKmh > DemoScenarios.ReferenceSpeedLimitKmh
                                                       && phase.DurationSeconds >= DemoScenarios.ReferenceSpeedingDurationSeconds + marginSeconds);
            var stopCoverage = phases.Any(phase => phase.Kind == ScenarioPhaseKind.Stopped
                                                   && phase.DurationSeconds >= DemoScenarios.ReferenceStopDurationSeconds + marginSeconds);
            if (!speedingCoverage || !stopCoverage)
            {
                errors.Add(SimulatorConstants.InsufficientDemoCoverage);
            }

            // Route constructors validate the local coordinates and adjacent vertices.
            try
            {
                DemoRoutes.Create(index);
            }
            catch (ArgumentException exception)
            {
                errors.Add(exception.Message);
            }
        }
    }
}
