using GpsTracking.Application.Tracking;
using GpsTracking.Domain.Constants;
using GpsTracking.Domain.Models;
using GpsTracking.Domain.Rules;
using GpsTracking.Infrastructure.Storage;
using GpsTracking.Simulator.Movement;

namespace GpsTracking.Tests;

public sealed class GeofenceRuleTests
{
    private static readonly DateTimeOffset Start = DateTimeOffset.Parse("2026-10-19T12:00:00Z");
    private static readonly GeofenceDefinition Zone = new()
    {
        Id = "zone", Name = "Test zone", MinLatitude = 1, MaxLatitude = 2,
        MinLongitude = 1, MaxLongitude = 2
    };

    private static GpsSample Sample(int second, bool inside, string id = "one", double speed = 0)
        => new(id, Start.AddSeconds(second), 1.5, inside ? 1.5 : 0.5, speed, 90);

    [Theory]
    [InlineData(1.5, 1.5, GeofenceState.Inside)]
    [InlineData(1, 1, GeofenceState.Inside)]
    [InlineData(2, 2, GeofenceState.Inside)]
    [InlineData(1, 1.5, GeofenceState.Inside)]
    [InlineData(2, 1.5, GeofenceState.Inside)]
    [InlineData(1.5, 1, GeofenceState.Inside)]
    [InlineData(1.5, 2, GeofenceState.Inside)]
    [InlineData(0.99, 1.5, GeofenceState.Outside)]
    [InlineData(1.5, 2.01, GeofenceState.Outside)]
    public void InitializesAfterThreeSamplesWithoutAlert(double latitude, double longitude, GeofenceState expected)
    {
        var rule = new GeofenceRule(Zone);
        for (var tick = 0; tick < 3; tick++)
        {
            var sample = new GpsSample("one", Start.AddSeconds(tick * 10), latitude, longitude, 0, 0);
            Assert.Null(rule.Observe(sample, Start));
            Assert.Equal(tick < 2 ? GeofenceState.Unknown : expected, rule.Membership.State);
        }
    }

    [Fact]
    public void RollingMajorityConfirmsTransitionsOnceAtConfirmingGpsTime()
    {
        var rule = new GeofenceRule(Zone);
        foreach (var second in new[] { 0, 10, 20 })
        {
            Assert.Null(rule.Observe(Sample(second, false), Start));
        }
        Assert.Null(rule.Observe(Sample(30, true), Start));

        var entry = rule.Observe(Sample(40, true), Start.AddMinutes(1));
        Assert.Equal(GeofenceConstants.EnteredAlertType, entry!.Type);
        Assert.Equal(Start.AddSeconds(40), entry.OccurredAt);
        Assert.Equal(Start.AddMinutes(1), entry.CreatedAt);
        Assert.Equal(Zone.Id, entry.GeofenceId);
        Assert.Null(entry.SpeedLimitKmh);
        Assert.Null(rule.Observe(Sample(50, true), Start));
        Assert.Null(rule.Observe(Sample(60, false), Start));

        var exit = rule.Observe(Sample(70, false), Start);
        Assert.Equal(GeofenceConstants.ExitedAlertType, exit!.Type);
        Assert.NotEqual(entry.Id, exit.Id);
        Assert.Null(rule.Observe(Sample(80, false), Start));
    }

    [Fact]
    public void IsolatedContraryObservationAndUnobservedCrossingDoNotAlert()
    {
        var rule = new GeofenceRule(Zone);
        foreach (var second in new[] { 0, 10, 20 })
        {
            rule.Observe(Sample(second, false), Start);
        }

        Assert.Null(rule.Observe(Sample(30, true), Start));
        Assert.Null(rule.Observe(Sample(40, false), Start));
        Assert.Null(rule.Observe(Sample(50, false), Start));
        // The line crosses the rectangle, but both recorded endpoints are outside.
        Assert.Null(rule.Observe(new GpsSample("one", Start.AddSeconds(60), 1.5, 2.5, 0, 90), Start));
        Assert.Equal(GeofenceState.Outside, rule.Membership.State);
    }

    [Fact]
    public void StoreHandlesOverlappingZonesAndSpeedingAtomicallyAndRejectsDuplicateSamples()
    {
        var store = new InMemoryTrackingStore(
            [new VehicleDefinition { Id = "one" }, new VehicleDefinition { Id = "two" }],
            geofenceDefinitions: [Zone, Zone with { Id = "overlap" }]);
        foreach (var second in new[] { 0, 10, 20 }) store.Append(Sample(second, false), Start);
        var before = store.GetSnapshots()[0];
        Assert.Empty(before.Alerts!);
        Assert.Equal(SampleAcceptance.TimestampNotNewer, store.Append(Sample(20, true), Start));
        store.Append(Sample(30, false, speed: 85), Start);
        store.Append(Sample(40, false, speed: 85), Start);
        store.Append(Sample(50, true, speed: 85), Start);

        var accepted = store.AppendWithSnapshot(Sample(60, true, speed: 85), Start);
        var snapshot = accepted.Snapshot!;

        Assert.Equal(7, snapshot.Revision);
        Assert.Equal(7, snapshot.SampleCount);
        Assert.Equal(3, snapshot.Alerts!.Count);
        Assert.Single(snapshot.Alerts, alert => alert.Type == SettingsConstants.SpeedingAlertType);
        Assert.All(snapshot.GeofenceMemberships!, membership => Assert.Equal(GeofenceState.Inside, membership.State));
        Assert.All(before.GeofenceMemberships!, membership => Assert.Equal(GeofenceState.Outside, membership.State));
        Assert.All(store.GetSnapshots()[1].GeofenceMemberships!, membership => Assert.Equal(GeofenceState.Unknown, membership.State));

        var settings = new VehicleSettings { StopDurationSeconds = 20 };
        var updated = store.UpdateSettings("one", settings, Start)!;
        Assert.Equal(snapshot.GeofenceMemberships, updated.GeofenceMemberships);
        Assert.Equal(3, updated.Alerts!.Count);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void ExistingDemoRoutesProduceEntriesAndExitsAtTenSecondIntervals(int vehicleIndex)
    {
        var zones = new[]
        {
            Zone with { Id = "west", MinLatitude = 42.695, MaxLatitude = 42.700, MinLongitude = 23.315, MaxLongitude = 23.320 },
            Zone with { Id = "east", MinLatitude = 42.695, MaxLatitude = 42.700, MinLongitude = 23.325, MaxLongitude = 23.330 }
        };
        var route = DemoRoutes.Create(vehicleIndex);
        var rules = zones.Select(zone => new GeofenceRule(zone)).ToArray();
        var alerts = new List<TrackingAlert>();
        for (var second = 0; second <= 600; second += 10)
        {
            var position = route.Advance(TimeSpan.FromSeconds(second == 0 ? 0 : 10));
            var sample = new GpsSample("one", Start.AddSeconds(second), position.Latitude, position.Longitude,
                position.SpeedKmh, position.Heading);
            foreach (var rule in rules)
            {
                var alert = rule.Observe(sample, Start);
                if (alert is not null) alerts.Add(alert);
            }
        }

        foreach (var zone in zones)
        {
            Assert.Contains(alerts, alert => alert.GeofenceId == zone.Id && alert.Type == GeofenceConstants.EnteredAlertType);
            Assert.Contains(alerts, alert => alert.GeofenceId == zone.Id && alert.Type == GeofenceConstants.ExitedAlertType);
        }
    }
}
