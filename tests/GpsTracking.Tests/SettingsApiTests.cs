using System.Net;
using System.Net.Http.Json;
using GpsTracking.Contracts;
using GpsTracking.Contracts.Tracking;
using Microsoft.AspNetCore.Mvc.Testing;

namespace GpsTracking.Tests;

public sealed class SettingsApiTests
{
    private const string VehicleSettingsPath = "api/vehicles/vehicle-1/settings";
    private const string GlobalSettingsPath = "api/settings";
    private const string AlertsPath = "api/alerts";

    [Fact]
    public async Task SettingsImmediatelyCreateAlertAndPersistAcrossClients()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();
        var start = DateTimeOffset.UtcNow.AddMinutes(-1);
        foreach (var second in new[] { 0, 10, 20 })
        {
            using var accepted = await client.PostAsJsonAsync(ApiRoutes.SamplesFor("vehicle-1"),
                new SubmitGpsSampleRequest { Timestamp = start.AddSeconds(second), Latitude = 42,
                    Longitude = 23, SpeedKmh = 85, Heading = 90 });
            Assert.Equal(HttpStatusCode.NoContent, accepted.StatusCode);
        }

        var before = await client.GetFromJsonAsync<AlertsResponse>(AlertsPath);
        Assert.Empty(before!.Alerts);

        var settings = new UpdateVehicleSettingsRequest
        {
            SpeedLimitKmh = 80,
            SpeedingDurationSeconds = 15,
            StopDurationSeconds = 180
        };

        using var saved = await client.PutAsJsonAsync(VehicleSettingsPath, settings);
        Assert.Equal(HttpStatusCode.OK, saved.StatusCode);

        var result = await saved.Content.ReadFromJsonAsync<VehicleUpdatedEvent>();
        var alert = Assert.Single(result!.Vehicle.Alerts!);

        Assert.Equal(start.AddSeconds(20), alert.OccurredAt);
        Assert.Equal(3, result.Vehicle.SampleCount);
        Assert.Equal(4, result.Vehicle.Revision);

        using var repeated = await client.PutAsJsonAsync(VehicleSettingsPath, settings);
        using var refreshed = factory.CreateClient();
        var history = await refreshed.GetFromJsonAsync<AlertsResponse>(AlertsPath);

        Assert.Equal(alert.Id, Assert.Single(history!.Alerts).Id);

        var otherVehicle = await refreshed.GetFromJsonAsync<VehicleSnapshotResponse>("api/vehicles/vehicle-2/settings");
        Assert.Equal(30, otherVehicle!.Settings!.SpeedingDurationSeconds);

        using var radius = await client.PutAsJsonAsync(GlobalSettingsPath, new UpdateGlobalSettingsRequest
        {
            StopRadiusMeters = 25
        });

        Assert.Equal(HttpStatusCode.OK, radius.StatusCode);

        var global = await refreshed.GetFromJsonAsync<GlobalSettingsEvent>(GlobalSettingsPath);
        Assert.Equal(25, global!.Settings.StopRadiusMeters);
        Assert.Equal(1, global.Settings.Revision);
        Assert.Equal(history.SessionId, global.SessionId);
    }

    [Fact]
    public async Task InvalidSettingsAndUnknownVehiclesDoNotMutateState()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();
        var before = await client.GetFromJsonAsync<TrackingSnapshotResponse>(ApiRoutes.VehiclesPath);
        var valid = new UpdateVehicleSettingsRequest
            { SpeedLimitKmh = 80, SpeedingDurationSeconds = 30, StopDurationSeconds = 180 };

        foreach (var invalid in new[] { valid with
                     {
                         SpeedLimitKmh = 0
                     }, valid with
                     {
                         SpeedingDurationSeconds = -1
                     },
                     valid with
                     {
                         StopDurationSeconds = null
                     } })
        {
            using var response = await client.PutAsJsonAsync(VehicleSettingsPath, invalid);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        using var unknown = await client.PutAsJsonAsync("api/vehicles/missing/settings", valid);
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);

        using var invalidGlobal = await client.PutAsJsonAsync(GlobalSettingsPath, new UpdateGlobalSettingsRequest { StopRadiusMeters = -1 });
        Assert.Equal(HttpStatusCode.BadRequest, invalidGlobal.StatusCode);

        var after = await client.GetFromJsonAsync<TrackingSnapshotResponse>(ApiRoutes.VehiclesPath);
        Assert.Equal(before!.GlobalSettings, after!.GlobalSettings);
        Assert.All(after.Vehicles, vehicle => Assert.Equal(0, vehicle.Revision));
    }
}
