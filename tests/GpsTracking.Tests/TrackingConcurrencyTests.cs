using System.Text.Json;
using GpsTracking.Application.Tracking;
using GpsTracking.Domain.Models;
using GpsTracking.Infrastructure.Storage;

namespace GpsTracking.Tests;

public sealed class TrackingConcurrencyTests
{
    [Fact]
    public async Task ConcurrentSamplesSettingsAndReadsPreserveAtomicStateAndCompletedRoutes()
    {
        var start = DateTimeOffset.Parse("2026-10-22T12:00:00Z");
        var ids = new[] { "vehicle-1", "vehicle-2" };
        var store = new InMemoryTrackingStore(ids.Select(id => new VehicleDefinition { Id = id, Name = id }));
        foreach (var id in ids)
        {
            foreach (var (second, longitude) in new[] { (0, 23d), (10, 23.001), (20, 23.001), (200, 23.001), (210, 23.002) })
            {
                store.Append(new GpsSample(id, start.AddSeconds(second), 42, longitude, 95, 90), start);
            }
        }

        var completedBefore = store.GetSnapshots().ToDictionary(vehicle => vehicle.Id,
            vehicle => JsonSerializer.Serialize(vehicle.Segments!.Where(segment => segment.EndedAt is not null)));
        Assert.All(store.GetSnapshots(), vehicle => Assert.Single(vehicle.Segments!, segment => segment.EndedAt is not null));

        const int rounds = 40;
        using var barrier = new Barrier(5);
        void Rendezvous() => Assert.True(barrier.SignalAndWait(TimeSpan.FromSeconds(10)), "Concurrent operation did not complete.");

        Task Write(string id) => Run(round =>
        {
            var sample = new GpsSample(id, start.AddSeconds(220 + round * 10), 42, 23.003 + round * .001, 95, 90);
            Assert.Equal(SampleAcceptance.Accepted, store.Append(sample, start));
        });

        var globalWriter = Run(round =>
        {
            var changed = store.UpdateGlobalSettings(new GlobalSettings { StopRadiusMeters = round % 2 == 0 ? 20 : 10 });
            Assert.Equal(round + 1, changed.Revision);
            Assert.Equal(2, changed.Vehicles!.Count);
        });
        var vehicleWriter = Run(round => store.UpdateSettings(ids[0], new VehicleSettings
        {
            SpeedLimitKmh = round % 2 == 0 ? 90 : 80
        }, start));
        var reader = Run(round =>
        {
            var state = store.GetState();
            foreach (var vehicle in state.Vehicles)
            {
                // Global revision must be reflected in BOTH captured vehicle revisions.
                var ownSettingsChanges = vehicle.Id == ids[0] ? vehicle.Revision - vehicle.SampleCount - state.GlobalSettings.Revision : 0;
                Assert.InRange(ownSettingsChanges, vehicle.Id == ids[0] ? round : 0, vehicle.Id == ids[0] ? round + 1 : 0);
                Assert.Equal(vehicle.SampleCount + state.GlobalSettings.Revision + ownSettingsChanges, vehicle.Revision);
                Assert.InRange(vehicle.SampleCount, 5 + round, 6 + round);
                Assert.Single(vehicle.Alerts!);
                Assert.Equal(completedBefore[vehicle.Id], JsonSerializer.Serialize(vehicle.Segments!.Where(segment => segment.EndedAt is not null)));
            }
        });

        await Task.WhenAll(Write(ids[0]), Write(ids[1]), globalWriter, vehicleWriter, reader).WaitAsync(TimeSpan.FromSeconds(30));
        foreach (var vehicle in store.GetState().Vehicles)
        {
            Assert.Equal(rounds + 5, vehicle.SampleCount);
            Assert.Equal(rounds + 5 + rounds + (vehicle.Id == ids[0] ? rounds : 0), vehicle.Revision);
            Assert.Equal(rounds + 5, store.GetHistory(vehicle.Id)!.Samples.Count);
            Assert.Single(vehicle.Alerts!);
        }

        Task Run(Action<int> action) => Task.Factory.StartNew(() =>
        {
            for (var round = 0; round < rounds; round++)
            {
                Rendezvous();
                action(round);
                Rendezvous();
            }
        }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }
}
