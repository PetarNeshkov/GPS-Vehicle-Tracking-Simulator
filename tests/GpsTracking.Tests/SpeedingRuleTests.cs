using GpsTracking.Domain.Models;
using GpsTracking.Domain.Rules;
using GpsTracking.Infrastructure.Storage;

namespace GpsTracking.Tests;

public sealed class SpeedingRuleTests
{
    private static readonly DateTimeOffset Start = new(2026, 10, 19, 12, 0, 0, TimeSpan.Zero);
    private static GpsSample Sample(int seconds, double speed = 85, string id = "vehicle-1")
        => new(id, Start.AddSeconds(seconds), 42, 23, speed, 90);

    [Fact]
    public void RequiresFullDurationAndRearmsAtThresholdEquality()
    {
        var rule = new SpeedingRule();
        var settings = new VehicleSettings();
        foreach (var second in new[] { 0, 10, 20 })
        {
            Assert.Null(rule.Observe(Sample(second), settings, Start));
        }

        var alert = rule.Observe(Sample(30), settings, Start.AddHours(1));
        Assert.NotNull(alert);
        Assert.Equal(Start.AddSeconds(30), alert.OccurredAt);
        Assert.Equal(Start.AddHours(1), alert.CreatedAt);
        Assert.Null(rule.Observe(Sample(40), settings, Start));
        Assert.Null(rule.Observe(Sample(50, 80), settings, Start));

        foreach (var second in new[] { 60, 70, 80 })
        {
            Assert.Null(rule.Observe(Sample(second), settings, Start));
        }

        Assert.NotNull(rule.Observe(Sample(90), settings, Start));
    }

    [Fact]
    public void SettingsReevaluateCurrentSuffixWithoutReplayingPastEpisodes()
    {
        var history = new List<GpsSample> { Sample(0), Sample(10), Sample(20) };
        var rule = new SpeedingRule();
        var settings = new VehicleSettings();
        foreach (var sample in history)
        {
            Assert.Null(rule.Observe(sample, settings, Start));
        }

        var shorter = settings with { SpeedingDurationSeconds = 15 };
        var alert = rule.Reevaluate(history, shorter, Start.AddMinutes(1));
        Assert.Equal(Start.AddSeconds(20), alert!.OccurredAt);
        Assert.Null(rule.Reevaluate(history, shorter, Start));
        Assert.Null(rule.Reevaluate(history, settings, Start));
        Assert.NotNull(rule.Reevaluate(history, shorter, Start));

        history.Add(Sample(30, 70));
        Assert.Null(rule.Reevaluate(history, shorter, Start));
        Assert.Null(rule.Reevaluate(history, shorter with { SpeedLimitKmh = 90 }, Start));
    }

    [Fact]
    public void SettingsAndSamplesPreserveAlertHistoryAndVehicleIndependence()
    {
        var store = new InMemoryTrackingStore([
            new VehicleDefinition { Id = "vehicle-1", Name = "First" },
            new VehicleDefinition { Id = "vehicle-2", Name = "Second" }
        ]);

        foreach (var second in new[] { 0, 10, 20 })
        {
            store.Append(Sample(second), Start);
        }

        var changed = store.UpdateSettings("vehicle-1", new VehicleSettings
        {
            SpeedingDurationSeconds = 15
        }, Start)!;

        var alert = Assert.Single(changed.Alerts!);
        Assert.Equal(3, changed.SampleCount);
        Assert.Equal(4, changed.Revision);
        Assert.Empty(store.GetSnapshots()[1].Alerts!);

        var unrelated = store.UpdateSettings("vehicle-1", changed.Settings! with { StopDurationSeconds = 200 }, Start)!;
        Assert.Equal(alert, Assert.Single(unrelated.Alerts!));

        var radius = store.UpdateGlobalSettings(new GlobalSettings { StopRadiusMeters = 25 });
        Assert.Equal(1, radius.Revision);
        Assert.Equal(alert, Assert.Single(store.GetSnapshots()[0].Alerts!));
        Assert.Equal(10, new InMemoryTrackingStore([]).GetGlobalSettings().Settings.StopRadiusMeters);
    }

    [Fact]
    public void ThresholdChangesRebuildCoverageFromRetainedObservations()
    {
        var rule = new SpeedingRule();
        var settings = new VehicleSettings();
        var history = new[]
        {
            Sample(0),
            Sample(10, 75),
            Sample(20, 90),
            Sample(30, 90)
        };

        foreach (var sample in history)
        {
            Assert.Null(rule.Observe(sample, settings, Start));
        }

        var alert = rule.Reevaluate(history, settings with
        {
            SpeedLimitKmh = 70
        }, Start);

        Assert.NotNull(alert);
        Assert.Equal(Start.AddSeconds(30), alert.OccurredAt);
        Assert.Equal(70, alert.SpeedLimitKmh);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void InvalidThresholdsAreRejected(double value)
    {
        Assert.NotNull(new VehicleSettings { SpeedLimitKmh = value }.Validate());
        Assert.NotNull(new GlobalSettings { StopRadiusMeters = value }.Validate());
    }

    [Fact]
    public async Task ConcurrentSettingsAndSamplesDoNotDuplicateQualifyingEpisodes()
    {
        var store = new InMemoryTrackingStore([new VehicleDefinition
        {
            Id = "vehicle-1", Name = "First"
        }]);

        var writer = Task.Run(() =>
        {
            for (var index = 0; index < 100; index++)
            {
                store.Append(Sample(index * 10), Start);
            }
        });

        var editor = Task.Run(() =>
        {
            for (var index = 0; index < 100; index++)
            {
                store.UpdateSettings("vehicle-1", new VehicleSettings
                {
                    StopDurationSeconds = 180 + index
                }, Start);
            }
        });
        await Task.WhenAll(writer, editor);

        var snapshot = store.GetSnapshots()[0];
        Assert.Equal(100, snapshot.SampleCount);
        Assert.Single(snapshot.Alerts!);
    }
}
