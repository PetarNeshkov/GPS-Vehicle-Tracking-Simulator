using System.Net;
using System.Net.Http.Json;
using GpsTracking.Contracts.Tracking;
using Microsoft.AspNetCore.Mvc.Testing;

namespace GpsTracking.Tests;

public sealed class MovementApiTests
{
    [Fact]
    public async Task HistoryAndRoutesPreserveStopBoundaryAndResetOnNewBackendSession()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();
        var empty = await client.GetFromJsonAsync<HistoryResponse>("api/vehicles/vehicle-1/history");
        Assert.Empty(empty!.Samples);

        using var unknownHistory = await client.GetAsync("api/vehicles/missing/history");
        using var unknownSegments = await client.GetAsync("api/vehicles/missing/segments");

        Assert.Equal(HttpStatusCode.NotFound, unknownHistory.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, unknownSegments.StatusCode);
        var start = DateTimeOffset.Parse("2026-10-20T12:00:00Z");
        foreach (var (second, longitude) in new[]
                 {
                     (-10, 23.310), (0, 23.312), (10, 23.312), (20, 23.312)
                 })
        {
            using var result = await client.PostAsJsonAsync("api/vehicles/vehicle-1/samples", new SubmitGpsSampleRequest
            {
                Timestamp = start.AddSeconds(second), Latitude = 42.697, Longitude = longitude, SpeedKmh = 0, Heading = 90
            });

            Assert.Equal(HttpStatusCode.NoContent, result.StatusCode);
        }

        using var settings = await client.PutAsJsonAsync("api/vehicles/vehicle-1/settings", new UpdateVehicleSettingsRequest
        {
            SpeedLimitKmh = 80, SpeedingDurationSeconds = 30, StopDurationSeconds = 15
        });
        var changed = await settings.Content.ReadFromJsonAsync<VehicleUpdatedEvent>();
        Assert.Equal("Stopped", changed!.Vehicle.Movement!.State);

        var closed = Assert.Single(changed.Vehicle.Segments!);
        Assert.Equal(start, closed.EndedAt);
        Assert.Equal(2, closed.Points.Count);

        using var duplicate = await client.PostAsJsonAsync("api/vehicles/vehicle-1/samples", new SubmitGpsSampleRequest
        {
            Timestamp = start.AddSeconds(20), Latitude = 42.697, Longitude = 23.320, SpeedKmh = 50, Heading = 90
        });
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);

        using var resume = await client.PostAsJsonAsync("api/vehicles/vehicle-1/samples", new SubmitGpsSampleRequest
        {
            Timestamp = start.AddMinutes(5), Latitude = 42.697, Longitude = 23.314, SpeedKmh = 50, Heading = 90
        });
        Assert.Equal(HttpStatusCode.NoContent, resume.StatusCode);

        using var refreshed = factory.CreateClient();
        var history = await refreshed.GetFromJsonAsync<HistoryResponse>("api/vehicles/vehicle-1/history");
        var routes = await refreshed.GetFromJsonAsync<SegmentsResponse>("api/vehicles/vehicle-1/segments");
        Assert.Equal(5, history!.Samples.Count);
        Assert.Equal(history.SessionId, routes!.SessionId);
        Assert.Equal(history.Revision, routes.Revision);
        Assert.Equal(2, routes.Segments.Count);
        Assert.Equal(closed.Id, routes.Segments[0].Id);
        Assert.Equal(start.AddMinutes(5), routes.Segments[1].StartedAt);
        Assert.Single(routes.Segments[1].Points);

        using var radius = await client.PutAsJsonAsync("api/settings", new UpdateGlobalSettingsRequest { StopRadiusMeters = 20 });
        var global = await radius.Content.ReadFromJsonAsync<GlobalSettingsEvent>();
        Assert.Equal(2, global!.Vehicles!.Count);
        Assert.Equal(closed.Id, global.Vehicles[0].Segments![0].Id);
        Assert.Equal(start, global.Vehicles[0].Segments![0].EndedAt);

        await using var restartedFactory = new WebApplicationFactory<Program>();
        using var restarted = restartedFactory.CreateClient();
        var reset = await restarted.GetFromJsonAsync<TrackingSnapshotResponse>("api/vehicles");
        Assert.NotEqual(history.SessionId, reset!.SessionId);
        Assert.All(reset.Vehicles, vehicle => { Assert.Empty(vehicle.Segments!); Assert.Equal("Unknown", vehicle.Movement!.State); });
    }
}
