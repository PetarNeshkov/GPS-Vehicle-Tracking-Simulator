using System.Net;
using System.Net.Http.Json;
using GpsTracking.Contracts;
using GpsTracking.Contracts.Tracking;
using GpsTracking.Simulator.Movement;
using Microsoft.AspNetCore.Mvc.Testing;

namespace GpsTracking.Tests;

public sealed class SimulationSettingsTests
{
    [Fact]
    public async Task ProfilesAreIndependentValidatedAndDoNotChangeAlertThresholds()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();
        var firstPath = ApiRoutes.SimulationFor("vehicle-1");
        var first = await client.GetFromJsonAsync<SimulationProfile>(firstPath);
        Assert.Equal(SimulationProfile.DefaultFor(0), first);

        var updated = first! with { CruisingSpeedKmh = 110, StopSeconds = 20 };
        using var saved = await client.PutAsJsonAsync(firstPath, updated);
        Assert.Equal(HttpStatusCode.OK, saved.StatusCode);
        Assert.Equal(updated, await client.GetFromJsonAsync<SimulationProfile>(firstPath));
        Assert.Equal(SimulationProfile.DefaultFor(1),
            await client.GetFromJsonAsync<SimulationProfile>(ApiRoutes.SimulationFor("vehicle-2")));

        using var rejected = await client.PutAsJsonAsync(firstPath, updated with { DrivingSeconds = 0 });
        Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);
        Assert.Equal(updated, await client.GetFromJsonAsync<SimulationProfile>(firstPath));
        using var unknown = await client.PutAsJsonAsync(ApiRoutes.SimulationFor("missing"), updated);
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);

        var tracking = await client.GetFromJsonAsync<TrackingSnapshotResponse>(ApiRoutes.VehiclesPath);
        Assert.All(tracking!.Vehicles, vehicle =>
        {
            Assert.Equal(80, vehicle.Settings!.SpeedLimitKmh);
            Assert.Equal(0, vehicle.SampleCount);
        });
    }

    [Fact]
    public void ApplyingNewProfilePreservesPositionAndRestartsOnlyCurrentPhaseTimer()
    {
        var scenario = DemoScenarios.Create(0);
        var before = scenario.Advance(TimeSpan.FromSeconds(125));
        Assert.Equal(ScenarioPhaseKind.Speeding, before.Phase);
        var profile = SimulationProfile.DefaultFor(0) with { SpeedingSpeedKmh = 105, SpeedingSeconds = 20 };

        scenario.ApplyProfile(profile);
        var changed = scenario.Advance(TimeSpan.Zero);
        Assert.Equal(before.Position.Latitude, changed.Position.Latitude);
        Assert.Equal(before.Position.Longitude, changed.Position.Longitude);
        Assert.Equal(105, changed.Position.SpeedKmh);

        Assert.Equal(ScenarioPhaseKind.Speeding, scenario.Advance(TimeSpan.FromSeconds(10)).Phase);
        scenario.ApplyProfile(profile); // Repeated polling must not restart the timer.
        Assert.Equal(ScenarioPhaseKind.Driving, scenario.Advance(TimeSpan.FromSeconds(10)).Phase);
    }

    [Fact]
    public void InvalidProfileDoesNotReplaceRunningScenario()
    {
        var scenario = DemoScenarios.Create(0);
        var before = scenario.Advance(TimeSpan.FromSeconds(10));
        Assert.Throws<ArgumentException>(() => scenario.ApplyProfile(
            SimulationProfile.DefaultFor(0) with { SpeedingSpeedKmh = double.NaN }));
        Assert.Equal(before, scenario.Advance(TimeSpan.Zero));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void AdjustedDurationsAdvanceThroughEveryPhase(int index)
    {
        var profile = SimulationProfile.DefaultFor(index) with
        {
            DrivingSeconds = 10, SpeedingSeconds = 10, RecoverySeconds = 10,
            StopSeconds = 10, ResumeSeconds = 10
        };
        var scenario = DemoScenarios.Create(index);
        scenario.ApplyProfile(profile);

        Assert.Equal(ScenarioPhaseKind.Speeding, scenario.Advance(TimeSpan.FromSeconds(10)).Phase);
        Assert.Equal(ScenarioPhaseKind.Driving, scenario.Advance(TimeSpan.FromSeconds(10)).Phase);
        var stopped = scenario.Advance(TimeSpan.FromSeconds(10));

        Assert.Equal(ScenarioPhaseKind.Stopped, stopped.Phase);
        var resumed = scenario.Advance(TimeSpan.FromSeconds(10));
        Assert.Equal(ScenarioPhaseKind.Resuming, resumed.Phase);
        Assert.Equal(stopped.Position.Latitude, resumed.Position.Latitude);
        Assert.Equal(stopped.Position.Longitude, resumed.Position.Longitude);
        Assert.Equal(ScenarioPhaseKind.Driving, scenario.Advance(TimeSpan.FromSeconds(10)).Phase);
    }
}
