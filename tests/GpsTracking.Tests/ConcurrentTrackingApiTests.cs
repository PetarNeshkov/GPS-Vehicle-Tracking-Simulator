using System.Net;
using System.Net.Http.Json;
using GpsTracking.Contracts;
using GpsTracking.Contracts.Tracking;
using Microsoft.AspNetCore.Mvc.Testing;

namespace GpsTracking.Tests;

public sealed class ConcurrentTrackingApiTests
{
    [Fact]
    public async Task ConcurrentHttpRequestsPreserveSharedStateAcrossRequestScopes()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();
        var start = DateTimeOffset.Parse("2026-10-22T12:00:00Z");
        const int rounds = 20;

        for (var round = 0; round < rounds; round++)
        {
            var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            async Task Run(Func<Task> action) { await ready.Task; await action(); }

            async Task SendSample(string id)
            {
                using var response = await client.PostAsJsonAsync(ApiRoutes.SamplesFor(id), new SubmitGpsSampleRequest
                {
                    Timestamp = start.AddSeconds(round * 10), Latitude = 42,
                    Longitude = 23 + round * .001, SpeedKmh = 95, Heading = 90
                });
                Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            }

            var actions = new[]
            {
                Run(() => SendSample("vehicle-1")),
                Run(() => SendSample("vehicle-2")),
                Run(async () =>
                {
                    using var response = await client.PutAsJsonAsync("api/settings",
                        new UpdateGlobalSettingsRequest { StopRadiusMeters = round % 2 == 0 ? 20 : 10 });
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    var captured = await response.Content.ReadFromJsonAsync<GlobalSettingsEvent>();
                    Assert.Equal(round + 1, captured!.Settings.Revision);
                    Assert.Equal(2, captured.Vehicles!.Count);
                }),
                Run(async () =>
                {
                    using var response = await client.PutAsJsonAsync("api/vehicles/vehicle-1/settings",
                        new UpdateVehicleSettingsRequest
                        {
                            SpeedLimitKmh = round % 2 == 0 ? 90 : 80,
                            SpeedingDurationSeconds = 30, StopDurationSeconds = 180
                        });
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                }),
                Run(async () =>
                {
                    var snapshot = await client.GetFromJsonAsync<TrackingSnapshotResponse>(ApiRoutes.VehiclesPath);
                    var globalRevision = snapshot!.GlobalSettings!.Revision;
                    Assert.InRange(globalRevision, round, round + 1);
                    Assert.Equal(globalRevision % 2 == 0 ? 10 : 20, snapshot.GlobalSettings.StopRadiusMeters);
                    foreach (var vehicle in snapshot.Vehicles)
                    {
                        Assert.InRange(vehicle.SampleCount, round, round + 1);
                        var ownChanges = vehicle.Revision - vehicle.SampleCount - globalRevision;
                        Assert.InRange(ownChanges, vehicle.Id == "vehicle-1" ? round : 0,
                            vehicle.Id == "vehicle-1" ? round + 1 : 0);
                        Assert.Equal(ownChanges % 2 == 0 ? 80 : 90, vehicle.Settings!.SpeedLimitKmh);
                        Assert.InRange(vehicle.Alerts!.Count, 0, 1);
                    }
                })
            };
            ready.SetResult();
            await Task.WhenAll(actions).WaitAsync(TimeSpan.FromSeconds(10));
        }

        var final = await client.GetFromJsonAsync<TrackingSnapshotResponse>(ApiRoutes.VehiclesPath);
        foreach (var vehicle in final!.Vehicles)
        {
            Assert.Equal(rounds, vehicle.SampleCount);
            Assert.Equal(vehicle.Id == "vehicle-1" ? rounds * 3 : rounds * 2, vehicle.Revision);
            Assert.Single(vehicle.Alerts!);
            var history = await client.GetFromJsonAsync<HistoryResponse>($"api/vehicles/{vehicle.Id}/history");
            Assert.Equal(rounds, history!.Samples.Count);
            Assert.Equal(rounds, history.Samples.Select(sample => sample.Timestamp).Distinct().Count());
        }
    }
}
