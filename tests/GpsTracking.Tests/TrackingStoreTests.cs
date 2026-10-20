using System.Text.Json;
using GpsTracking.Application.Tracking;
using GpsTracking.Domain.Models;
using GpsTracking.Infrastructure.Storage;

namespace GpsTracking.Tests;

public sealed class TrackingStoreTests
{
    private static readonly DateTimeOffset Start = new(2026, 10, 18, 9, 0, 0, TimeSpan.Zero);

    private static InMemoryTrackingStore CreateStore() => new([
        new VehicleDefinition { Id = "vehicle-1", Name = "First" },
        new VehicleDefinition { Id = "vehicle-2", Name = "Second" }
    ]);

    private static GpsSample Sample(string id, int second) => new(id, Start.AddSeconds(second), 42, 23, 41, 90);

    [Fact]
    public void StartsWithBothVehiclesAndNoObservations()
    {
        var snapshots = CreateStore().GetSnapshots();
        Assert.Equal(2, snapshots.Count);
        Assert.All(snapshots, snapshot =>
        {
            Assert.Null(snapshot.LatestSample);
            Assert.Null(snapshot.LastReceivedAt);
            Assert.Equal(0, snapshot.SampleCount);
            Assert.Equal(0, snapshot.Revision);
        });
    }

    [Fact]
    public void RejectionsDoNotMutateStateAndSnapshotsRemainStable()
    {
        var store = CreateStore();
        Assert.Equal(SampleAcceptance.Accepted, store.Append(Sample("vehicle-1", 10), Start));
        var before = store.GetSnapshots();
        Assert.Equal(SampleAcceptance.UnknownVehicle, store.Append(Sample("unknown", 20), Start));
        Assert.Equal(SampleAcceptance.TimestampNotNewer, store.Append(Sample("vehicle-1", 10), Start));
        Assert.Equal(SampleAcceptance.TimestampNotNewer, store.Append(Sample("vehicle-1", 0), Start));
        Assert.Equal(JsonSerializer.Serialize(before),
            JsonSerializer.Serialize(store.GetSnapshots()));

        store.Append(Sample("vehicle-1", 20), Start.AddSeconds(20));
        Assert.Equal(1, before[0].SampleCount);
        Assert.Equal(2, store.GetSnapshots()[0].SampleCount);
        Assert.Equal(0, store.GetSnapshots()[1].SampleCount);
        Assert.Equal(0, CreateStore().GetSnapshots()[0].SampleCount);
    }

    [Fact]
    public async Task ConcurrentVehicleWritesAndReadsProduceConsistentSnapshots()
    {
        var store = CreateStore();
        var writers = new[] { "vehicle-1", "vehicle-2" }.Select(id => Task.Run(() =>
        {
            for (var i = 1; i <= 200; i++)
                Assert.Equal(SampleAcceptance.Accepted, store.Append(Sample(id, i), Start.AddSeconds(i)));
        }));
        var reader = Task.Run(() =>
        {
            for (var i = 0; i < 500; i++)
                foreach (var snapshot in store.GetSnapshots())
                {
                    Assert.Equal(snapshot.SampleCount, snapshot.Revision);
                    if (snapshot.LatestSample is { } sample)
                        Assert.Equal(Start.AddSeconds(snapshot.SampleCount), sample.Timestamp);
                }
        });
        await Task.WhenAll(writers.Append(reader));
        Assert.All(store.GetSnapshots(), snapshot => Assert.Equal(200, snapshot.SampleCount));
    }
}
