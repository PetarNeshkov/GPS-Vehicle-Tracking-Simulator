using GpsTracking.Simulator.Configuration;
using GpsTracking.Simulator.Movement;

namespace GpsTracking.Tests;

public sealed class ScenarioProgressorTests
{
    private static ScenarioProgressor Create()
        => new(new RouteProgressor(36,
            new RoutePoint(0, 0),
            new RoutePoint(0, 1)), [
            new ScenarioPhase(ScenarioPhaseKind.Driving, 10, 36),
            new ScenarioPhase(ScenarioPhaseKind.Stopped, 20, 0),
            new ScenarioPhase(ScenarioPhaseKind.Resuming, 10, 72)
        ]);

    [Fact]
    public void IntegratesAcrossPhaseBoundariesAndKeepsPositionWhenCycleRepeats()
    {
        var scenario = Create();
        var firstStop = scenario.Advance(TimeSpan.FromSeconds(10));
        Assert.Equal(ScenarioPhaseKind.Stopped, firstStop.Phase);
        Assert.Equal(0, firstStop.Position.SpeedKmh);
        Assert.InRange(firstStop.Position.Longitude, 0.000899, 0.000900);

        var dwelling = scenario.Advance(TimeSpan.FromSeconds(15));
        Assert.Equal(firstStop.Position, dwelling.Position);
        var resumed = scenario.Advance(TimeSpan.FromSeconds(10));
        Assert.Equal(ScenarioPhaseKind.Resuming, resumed.Phase);
        Assert.Equal(72, resumed.Position.SpeedKmh);
        Assert.InRange(resumed.Position.Longitude, 0.001798, 0.001800);

        var nextCycle = scenario.Advance(TimeSpan.FromSeconds(5));
        Assert.Equal(ScenarioPhaseKind.Driving, nextCycle.Phase);
        Assert.Equal(36, nextCycle.Position.SpeedKmh);
        Assert.InRange(nextCycle.Position.Longitude, 0.002697, 0.002699);
    }

    [Fact]
    public void LongElapsedTimeMatchesSmallStepsIncludingStopsAndRepeatedCycles()
    {
        var expected = Create().Advance(TimeSpan.FromSeconds(125));
        var scenario = Create();
        var actual = scenario.Advance(TimeSpan.Zero);
        for (var second = 0; second < 125; second++)
        {
            actual = scenario.Advance(TimeSpan.FromSeconds(1));
        }

        Assert.Equal(expected.Phase, actual.Phase);
        Assert.Equal(expected.Position.Longitude, actual.Position.Longitude, 10);
        Assert.Equal(expected.Position.SpeedKmh, actual.Position.SpeedKmh);
        Assert.Equal(expected.Position.Heading, actual.Position.Heading);
    }

    [Fact]
    public void VehicleScenariosHaveIndependentProgressAndDifferentStopTimes()
    {
        var first = DemoScenarios.Create(0);
        var second = DemoScenarios.Create(1);
        var originalSecond = second.Advance(TimeSpan.Zero);

        Assert.Equal(ScenarioPhaseKind.Stopped, first.Advance(TimeSpan.FromSeconds(210)).Phase);
        Assert.Equal(originalSecond, second.Advance(TimeSpan.Zero));
        Assert.Equal(ScenarioPhaseKind.Driving, second.Advance(TimeSpan.FromSeconds(210)).Phase);
    }

    [Fact]
    public void RejectsInvalidPhasesAndNegativeElapsedTime()
    {
        Assert.NotNull(ScenarioProgressor.Validate([]));
        Assert.NotNull(ScenarioProgressor.Validate([new(ScenarioPhaseKind.Driving, 0, 40)]));
        Assert.NotNull(ScenarioProgressor.Validate([new(ScenarioPhaseKind.Driving, 10, double.NaN)]));
        Assert.NotNull(ScenarioProgressor.Validate([new(ScenarioPhaseKind.Driving, 10, double.PositiveInfinity)]));
        Assert.NotNull(ScenarioProgressor.Validate([new(ScenarioPhaseKind.Stopped, 10, 20)]));
        Assert.NotNull(ScenarioProgressor.Validate([new(ScenarioPhaseKind.Driving, 10, 0)]));
        Assert.NotNull(ScenarioProgressor.Validate([new(ScenarioPhaseKind.Stopped, 10, 0)]));
        Assert.Throws<ArgumentOutOfRangeException>(() => Create().Advance(TimeSpan.FromSeconds(-1)));
    }

    [Theory]
    [InlineData(10, true)]
    [InlineData(15, true)]
    [InlineData(9, false)]
    [InlineData(30, false)]
    [InlineData(int.MaxValue, false)]
    public void StartupRequiresEnoughSampleCoverageForTheBuiltInDemonstration(int interval, bool valid)
    {
        var options = new SimulatorOptions
        {
            BackendBaseUrl = "http://localhost:5078", TickIntervalSeconds = interval,
            VehicleIds = ["vehicle-1", "vehicle-2"]
        };
        var validation = new SimulatorOptionsValidator().Validate(null, options);
        Assert.Equal(valid, validation.Succeeded);
    }
}
