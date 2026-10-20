using GpsTracking.Domain.Models;
using GpsTracking.Domain.Rules;
using GpsTracking.Infrastructure.Storage;

namespace GpsTracking.Tests;

public sealed class MovementRuleTests
{
    private static readonly DateTimeOffset Start = DateTimeOffset.Parse("2026-10-20T12:00:00Z");
    private static GpsSample Point(int seconds, double meters, string id = "one", double speed = 50)
        => new(id, Start.AddSeconds(seconds), 0, meters / 6_371_000 * 180 / Math.PI, speed, 90);

    [Fact]
    public void SettingsReevaluationPreservesKnownResumptionWithOneRoutePoint()
    {
        var rule = new MovementRule();
        rule.Observe(Point(0, 0), 10, 20);
        rule.Reevaluate(10, 30);
        Assert.Equal(MovementState.Unknown, rule.Movement.State);

        rule.Observe(Point(20, 0), 10, 20);
        Assert.Equal(MovementState.Stopped, rule.Movement.State);
        rule.Observe(Point(30, 30), 10, 20);
        var resumed = Assert.Single(rule.Segments);

        rule.Reevaluate(15, 60);

        Assert.Equal(MovementState.Moving, rule.Movement.State);
        var after = Assert.Single(rule.Segments);
        Assert.Equal(resumed.Id, after.Id);
        Assert.Equal(resumed.StartedAt, after.StartedAt);
        Assert.Single(after.Points);
        Assert.Null(after.EndedAt);
    }

    [Fact]
    public void ConfirmedStopEndsAtOnsetAndResumeStartsSeparateRoute()
    {
        var rule = new MovementRule();
        rule.Observe(Point(-10, -100), 10, 180);
        rule.Observe(Point(0, 0), 10, 180);
        for (var second = 10; second < 180; second += 10) rule.Observe(Point(second, 1), 10, 180);
        Assert.Equal(MovementState.PossibleStop, rule.Movement.State);
        Assert.Null(Assert.Single(rule.Segments).EndedAt);
        rule.Observe(Point(180, 0), 10, 180);
        var closed = Assert.Single(rule.Segments);
        Assert.Equal(Start, closed.EndedAt);
        Assert.Equal(2, closed.Points.Count);
        Assert.Equal(Start, closed.Points[^1].Timestamp);
        Assert.Equal(MovementState.Stopped, rule.Movement.State);
        rule.Observe(Point(290, 1), 10, 180);
        Assert.Single(rule.Segments);
        rule.Observe(Point(300, 30), 10, 180);
        Assert.Equal(MovementState.Moving, rule.Movement.State);
        Assert.Equal(closed, rule.Segments[0]);
        Assert.Single(rule.Segments[1].Points);
        Assert.Equal(Start.AddMinutes(5), rule.Segments[1].StartedAt);
        rule.Observe(Point(310, 60), 10, 180);
        Assert.Equal(2, rule.Segments[1].Points.Count);
    }

    [Fact]
    public void InitiallyStationaryVehicleDoesNotCreateDrivenRouteAndSpeedIsIrrelevant()
    {
        var rule = new MovementRule();
        rule.Observe(Point(0, 0, speed: 100), 10, 20);
        Assert.Equal(MovementState.Unknown, rule.Movement.State);
        rule.Observe(Point(10, 1, speed: 100), 10, 20);
        rule.Observe(Point(20, 0, speed: 100), 10, 20);
        Assert.Equal(MovementState.Stopped, rule.Movement.State);
        Assert.Empty(rule.Segments);
        rule.Observe(Point(30, 30, speed: 0), 10, 20);
        Assert.Equal(MovementState.Moving, rule.Movement.State);
        Assert.Single(Assert.Single(rule.Segments).Points);
    }

    [Fact]
    public void SlowDriftUsesFixedAnchorAndShortStopKeepsRouteId()
    {
        var rule = new MovementRule();
        rule.Observe(Point(0, 0), 10, 30);
        var id = Assert.Single(rule.Segments).Id;
        for (var second = 10; second <= 120; second += 10) rule.Observe(Point(second, second * 0.6), 10, 30);
        Assert.NotEqual(MovementState.Stopped, rule.Movement.State);
        Assert.Equal(id, Assert.Single(rule.Segments).Id);
        Assert.Null(rule.Segments[0].EndedAt);
    }

    [Fact]
    public void DistanceAtRadiusIsInsideAndSettingsRebuildLatestCandidateOnly()
    {
        var rule = new MovementRule();
        var first = Point(0, 0);
        var edge = Point(10, 10);
        var radius = GeographicDistance.Meters(first.Latitude, first.Longitude, edge.Latitude, edge.Longitude);
        rule.Observe(first, radius, 180);
        rule.Observe(edge, radius, 180);
        Assert.Equal(MovementState.PossibleStop, rule.Movement.State);
        rule.Observe(Point(20, 15), radius, 180);
        Assert.Equal(MovementState.Moving, rule.Movement.State);
        rule.Reevaluate(20, 15);
        Assert.Equal(MovementState.Stopped, rule.Movement.State);
        Assert.Equal(Start, rule.Movement.StopStartedAt);
        rule.Reevaluate(20, 300);
        Assert.Equal(MovementState.Stopped, rule.Movement.State);
        rule.Reevaluate(5, 300);
        Assert.Equal(MovementState.Moving, rule.Movement.State);
        Assert.Equal(Start.AddSeconds(20), Assert.Single(rule.Segments).StartedAt);
    }

    [Fact]
    public void DurationReevaluationClosesOnceAndPreservesCompletedBoundary()
    {
        var rule = new MovementRule();
        rule.Observe(Point(-10, -100), 10, 180);
        rule.Observe(Point(0, 0), 10, 180);
        rule.Observe(Point(20, 0), 10, 180);
        rule.Reevaluate(10, 15);
        var closed = Assert.Single(rule.Segments);
        rule.Reevaluate(1000, 300);
        Assert.Equal(closed, Assert.Single(rule.Segments));
        rule.Reevaluate(10, 15);
        Assert.Equal(closed, Assert.Single(rule.Segments));
    }

    [Fact]
    public void RadiusChangeReevaluatesBothVehiclesAndRetainsRawHistory()
    {
        var store = new InMemoryTrackingStore([
            new VehicleDefinition { Id = "one", Settings = new VehicleSettings { StopDurationSeconds = 20 } },
            new VehicleDefinition { Id = "two", Settings = new VehicleSettings { StopDurationSeconds = 20 } }
        ]);
        foreach (var id in new[] { "one", "two" })
        {
            store.Append(Point(0, 0, id), Start);
            store.Append(Point(10, 15, id), Start);
            store.Append(Point(20, 30, id), Start);
        }
        var before = store.GetState();
        var update = store.UpdateGlobalSettings(new GlobalSettings { StopRadiusMeters = 40 });
        Assert.Equal(2, update.Vehicles!.Count);
        Assert.All(update.Vehicles, vehicle =>
        {
            Assert.Equal(MovementState.Stopped, vehicle.Movement!.State);
            Assert.Equal(4, vehicle.Revision);
            Assert.Equal(3, vehicle.SampleCount);
            Assert.Equal(3, store.GetHistory(vehicle.Id)!.Samples.Count);
        });
        Assert.All(before.Vehicles, vehicle => Assert.Equal(MovementState.Moving, vehicle.Movement!.State));
        var repeated = store.UpdateGlobalSettings(new GlobalSettings { StopRadiusMeters = 40 });
        Assert.Equal(update.Revision, repeated.Revision);
        Assert.All(repeated.Vehicles!, vehicle => Assert.Equal(4, vehicle.Revision));
    }

    [Fact]
    public async Task ConcurrentRadiusChangesAndSamplesProduceConsistentSnapshots()
    {
        var store = new InMemoryTrackingStore([new VehicleDefinition { Id = "one" }, new VehicleDefinition { Id = "two" }]);
        var samples = Task.Run(() =>
        {
            for (var index = 0; index < 100; index++)
                foreach (var id in new[] { "one", "two" }) store.Append(Point(index * 10, index * 20, id), Start);
        });
        var changes = Task.Run(() =>
        {
            for (var index = 0; index < 100; index++)
            {
                var update = store.UpdateGlobalSettings(new GlobalSettings { StopRadiusMeters = 10 + index % 2 });
                Assert.All(update.Vehicles!, vehicle => Assert.True(vehicle.Revision >= vehicle.SampleCount));
            }
        });
        await Task.WhenAll(samples, changes).WaitAsync(TimeSpan.FromSeconds(10));
        Assert.All(store.GetState().Vehicles, vehicle => Assert.Equal(100, vehicle.SampleCount));
    }
}
