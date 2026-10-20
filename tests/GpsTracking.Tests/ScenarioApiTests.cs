using System.Net;
using System.Net.Http.Json;
using GpsTracking.Contracts.Tracking;
using GpsTracking.Domain.Constants;
using GpsTracking.Simulator.Movement;
using Microsoft.AspNetCore.Mvc.Testing;

namespace GpsTracking.Tests;

public sealed class ScenarioApiTests
{
    [Fact]
    public async Task NormalScenariosExerciseAllRulesWithDefaultSettingsAndRepeat()
    {
        Assert.Equal(SettingsConstants.DefaultSpeedLimitKmh, DemoScenarios.ReferenceSpeedLimitKmh);
        Assert.Equal(SettingsConstants.DefaultSpeedingDurationSeconds, DemoScenarios.ReferenceSpeedingDurationSeconds);
        Assert.Equal(SettingsConstants.DefaultStopDurationSeconds, DemoScenarios.ReferenceStopDurationSeconds);

        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();
        var scenarios = new[]
        {
            DemoScenarios.Create(0), DemoScenarios.Create(1)
        };
        var start = DateTimeOffset.Parse("2026-10-20T12:00:00Z");

        // Synthetic test clock only. The real worker uses real UTC and ten-second waits.
        for (var second = 0; second <= 1500; second += 10)
        {
            for (var index = 0; index < scenarios.Length; index++)
            {
                var elapsed = TimeSpan.FromSeconds(second == 0 ? 0 : 10);
                var sample = scenarios[index].Advance(elapsed).Position;
                using var accepted = await client.PostAsJsonAsync($"api/vehicles/vehicle-{index + 1}/samples",
                    new SubmitGpsSampleRequest
                {
                    Timestamp = start.AddSeconds(second), Latitude = sample.Latitude, Longitude = sample.Longitude,
                    SpeedKmh = sample.SpeedKmh, Heading = sample.Heading
                });
                Assert.Equal(HttpStatusCode.NoContent, accepted.StatusCode);
            }

            if (second == 390)
            {
                var duringStop = await client.GetFromJsonAsync<TrackingSnapshotResponse>("api/vehicles");
                Assert.Equal("Stopped", duringStop!.Vehicles[0].Movement!.State);
                Assert.Equal("Moving", duringStop.Vehicles[1].Movement!.State);
            }
        }

        var snapshot = await client.GetFromJsonAsync<TrackingSnapshotResponse>("api/vehicles");
        foreach (var vehicle in snapshot!.Vehicles)
        {
            var history = await client.GetFromJsonAsync<HistoryResponse>($"api/vehicles/{vehicle.Id}/history");
            Assert.Equal(151, history!.Samples.Count);

            var speedingAlerts = vehicle.Alerts!.Where(alert => alert.Type == SettingsConstants.SpeedingAlertType).ToArray();
            Assert.Equal(vehicle.Id == "vehicle-1" ? 3 : 2, speedingAlerts.Length);

            foreach (var zone in snapshot.Geofences!)
            {
                Assert.Contains(vehicle.Alerts!, alert => alert.GeofenceId == zone.Id && alert.Type == GeofenceConstants.EnteredAlertType);
                Assert.Contains(vehicle.Alerts!, alert => alert.GeofenceId == zone.Id && alert.Type == GeofenceConstants.ExitedAlertType);
            }

            var segments = vehicle.Segments!;
            var completed = segments.Where(segment => segment.EndedAt is not null).ToArray();
            Assert.True(completed.Length >= 2);
            Assert.Equal(segments.Count, segments.Select(segment => segment.Id).Distinct().Count());

            foreach (var segment in completed)
            {
                Assert.Equal(segment.EndedAt, segment.Points[^1].Timestamp);
                Assert.Contains(history.Samples, sample => sample.Timestamp > segment.EndedAt
                    && sample.Timestamp < segment.EndedAt.Value.AddSeconds(180)
                    && sample.Latitude == segment.Points[^1].Latitude && sample.Longitude == segment.Points[^1].Longitude);
            }

            var firstStopSecond = vehicle.Id == "vehicle-1" ? 210 : 420;
            Assert.Equal(start.AddSeconds(firstStopSecond), completed[0].EndedAt);

            // At an exact phase boundary there is no displacement yet; the next tick leaves the radius.
            var firstResumeSecond = vehicle.Id == "vehicle-1" ? 430 : 640;
            Assert.Equal(start.AddSeconds(firstResumeSecond), segments[1].StartedAt);
        }
    }
}
