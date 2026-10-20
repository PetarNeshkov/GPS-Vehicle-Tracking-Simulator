using GpsTracking.Simulator.Configuration;

namespace GpsTracking.Simulator.Movement;

public sealed class ScenarioProgressor
{
    private readonly RouteProgressor route;
    private ScenarioPhase[] phases;
    private int phaseIndex;
    private TimeSpan phaseElapsed;

    public ScenarioProgressor(RouteProgressor route, IEnumerable<ScenarioPhase> phases)
    {
        this.route = route;
        this.phases = phases.ToArray();
        var error = Validate(this.phases);

        if (error is not null)
        {
            throw new ArgumentException(error, nameof(phases));
        }
    }

    public static string? Validate(IReadOnlyList<ScenarioPhase> phases)
    {
        var invalid = phases.Count == 0 || phases.Any(phase => !Enum.IsDefined(phase.Kind)
            || phase.DurationSeconds <= 0
            || !double.IsFinite(phase.SpeedKmh) || phase.SpeedKmh < 0
            || (phase.Kind == ScenarioPhaseKind.Stopped) != (phase.SpeedKmh == 0));

        if (invalid || !phases.Any(phase => phase.SpeedKmh > 0))
        {
            return SimulatorConstants.InvalidPhases;
        }

        return null;
    }

    public ScenarioPosition Advance(TimeSpan elapsed)
    {
        if (elapsed < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(elapsed));
        }

        var remaining = elapsed;
        while (remaining > TimeSpan.Zero)
        {
            var phase = phases[phaseIndex];
            var untilBoundary = TimeSpan.FromSeconds(phase.DurationSeconds) - phaseElapsed;
            var step = remaining < untilBoundary ? remaining : untilBoundary;

            route.Advance(step, phase.SpeedKmh);
            remaining -= step;
            phaseElapsed += step;

            if (phaseElapsed >= TimeSpan.FromSeconds(phase.DurationSeconds))
            {
                phaseIndex = (phaseIndex + 1) % phases.Length;
                phaseElapsed = TimeSpan.Zero;
            }
        }

        // Report the current phase's instantaneous speed without moving twice.
        var current = phases[phaseIndex];
        var position = route.Advance(TimeSpan.Zero, current.SpeedKmh);

        return new ScenarioPosition(position, current.Kind);
    }

    public void ApplyProfile(GpsTracking.Contracts.Tracking.SimulationProfile profile)
    {
        var updatedPhases = DemoScenarios.FromProfile(profile).ToArray();
        var error = Validate(updatedPhases);
        if (error is not null)
        {
            throw new ArgumentException(error, nameof(profile));
        }

        if (phases.SequenceEqual(updatedPhases))
        {
            return;
        }

        phases = updatedPhases;
        phaseIndex %= phases.Length;
        phaseElapsed = TimeSpan.Zero;
    }
}
