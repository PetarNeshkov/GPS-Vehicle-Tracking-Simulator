using System.Net;
using System.Net.Http.Json;
using GpsTracking.Contracts;
using GpsTracking.Contracts.Tracking;
using GpsTracking.Domain.Constants;
using Microsoft.AspNetCore.Mvc.Testing;

namespace GpsTracking.Tests;

public sealed class GeofenceApiTests
{
    [Fact]
    public async Task DefinitionsMembershipAndHistorySurviveRefreshButResetWithBackend()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();
        var definitions = await client.GetFromJsonAsync<GeofencesResponse>("api/geofences");
        var initial = await client.GetFromJsonAsync<TrackingSnapshotResponse>(ApiRoutes.VehiclesPath);

        Assert.Equal(initial!.SessionId, definitions!.SessionId);
        Assert.Equal(2, definitions.Geofences.Count);
        Assert.Equal(definitions.Geofences, initial.Geofences);
        Assert.All(initial.Vehicles, vehicle => Assert.All(vehicle.GeofenceMemberships!, membership => Assert.Equal("Unknown", membership.State)));

        var start = DateTimeOffset.UtcNow;
        var longitudes = new[] { 23.312, 23.312, 23.312, 23.317, 23.317, 23.317, 23.312, 23.312 };
        for (var index = 0; index < longitudes.Length; index++)
        {
            using var response = await client.PostAsJsonAsync(ApiRoutes.SamplesFor("vehicle-1"), new SubmitGpsSampleRequest
            {
                Timestamp = start.AddSeconds(index * 10), Latitude = 42.697,
                Longitude = longitudes[index], SpeedKmh = 40, Heading = 90
            });

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        using var refreshed = factory.CreateClient();
        var snapshot = await refreshed.GetFromJsonAsync<TrackingSnapshotResponse>(ApiRoutes.VehiclesPath);
        var history = await refreshed.GetFromJsonAsync<AlertsResponse>("api/alerts");

        Assert.Equal(2, history!.Alerts.Count);
        Assert.Contains(history.Alerts, alert => alert.Type == GeofenceConstants.EnteredAlertType && alert.OccurredAt == start.AddSeconds(40));
        Assert.Contains(history.Alerts, alert => alert.Type == GeofenceConstants.ExitedAlertType && alert.OccurredAt == start.AddSeconds(70));
        Assert.All(history.Alerts, alert => Assert.Equal("sofia-west", alert.GeofenceId));
        Assert.All(snapshot!.Vehicles[0].GeofenceMemberships!, membership => Assert.Equal("Outside", membership.State));
        Assert.Equal(history.Alerts.Select(alert => alert.Id).Order(), snapshot.Vehicles[0].Alerts!.Select(alert => alert.Id).Order());

        await using var restartedFactory = new WebApplicationFactory<Program>();
        using var restarted = restartedFactory.CreateClient();
        var reset = await restarted.GetFromJsonAsync<TrackingSnapshotResponse>(ApiRoutes.VehiclesPath);
        Assert.NotEqual(initial.SessionId, reset!.SessionId);
        Assert.All(reset.Vehicles, vehicle =>
        {
            Assert.Empty(vehicle.Alerts!);
            Assert.All(vehicle.GeofenceMemberships!, membership => Assert.Equal("Unknown", membership.State));
        });
    }
}

