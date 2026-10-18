using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using GpsTracking.Contracts;
using GpsTracking.Api.Constants;
using GpsTracking.Contracts.Tracking;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace GpsTracking.Tests;

public sealed class TrackingApiTests
{
    [Fact]
    public async Task InheritedRoutesExposeHealthWithoutExposingBaseController()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        using var health = await client.GetAsync(ApiConstants.HealthRoute);
        Assert.Equal(HttpStatusCode.OK, health.StatusCode);
        var body = await health.Content.ReadFromJsonAsync<JsonObject>();
        Assert.Equal(ApiConstants.HealthyStatus, body!["status"]!.GetValue<string>());

        using var baseRoute = await client.GetAsync("api/baseapi");
        Assert.Equal(HttpStatusCode.NotFound, baseRoute.StatusCode);
        using var actionRoute = await client.GetAsync($"{ApiConstants.HealthRoute}/get");
        Assert.Equal(HttpStatusCode.NotFound, actionRoute.StatusCode);
    }

    private static WebApplicationFactory<Program> CreateFactory() =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder.UseEnvironment("Testing"));

    private static SubmitGpsSampleRequest Sample() => new()
    {
        Timestamp = new DateTimeOffset(2026, 10, 18, 12, 0, 0, TimeSpan.FromHours(3)),
        Latitude = 42.697, Longitude = 23.318, SpeedKmh = 85.5, Heading = 360
    };

    [Fact]
    public async Task AcceptsSamplesAndPreservesSessionAcrossHttpClients()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var initial = await client.GetFromJsonAsync<VehicleSnapshotResponse[]>(ApiRoutes.VehiclesPath);
        Assert.Equal(2, initial!.Length);
        Assert.All(initial, vehicle => Assert.Null(vehicle.LatestSample));
        using var response = await client.PostAsJsonAsync(ApiRoutes.SamplesFor("vehicle-1"), Sample());
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var refreshedClient = factory.CreateClient();
        var state = await refreshedClient.GetFromJsonAsync<VehicleSnapshotResponse[]>(ApiRoutes.VehiclesPath);
        var vehicle = Assert.Single(state!, item => item.Id == "vehicle-1");
        Assert.Equal(85.5, vehicle.LatestSample!.SpeedKmh);
        Assert.Equal(360, vehicle.LatestSample.Heading);
        Assert.Equal(TimeSpan.Zero, vehicle.LatestSample.Timestamp.Offset);
        Assert.Equal(1, vehicle.SampleCount);
        Assert.Equal(1, vehicle.Revision);
        Assert.NotNull(vehicle.LastReceivedAt);
        Assert.Null(Assert.Single(state!, item => item.Id == "vehicle-2").LatestSample);
    }

    [Theory]
    [InlineData("timestamp")]
    [InlineData("latitude")]
    [InlineData("longitude")]
    [InlineData("speedKmh")]
    [InlineData("heading")]
    public async Task MissingFieldsAreBadRequests(string field)
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var body = JsonSerializer.SerializeToNode(Sample(), new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        body.AsObject().Remove(field);
        using var content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json");
        using var response = await client.PostAsync(ApiRoutes.SamplesFor("vehicle-1"), content);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        var state = await client.GetFromJsonAsync<VehicleSnapshotResponse[]>(ApiRoutes.VehiclesPath);
        Assert.All(state!, vehicle => Assert.Equal(0, vehicle.SampleCount));
    }

    [Fact]
    public async Task InvalidUnknownAndNonNewerSamplesDoNotChangeAcceptedHistory()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var route = ApiRoutes.SamplesFor("vehicle-1");
        using var accepted = await client.PostAsJsonAsync(route, Sample());
        Assert.Equal(HttpStatusCode.NoContent, accepted.StatusCode);
        var cases = new[]
        {
            (route, Sample(), HttpStatusCode.Conflict),
            (route, Sample() with { Timestamp = Sample().Timestamp!.Value.AddSeconds(-10) }, HttpStatusCode.Conflict),
            (route, Sample() with { Latitude = 91 }, HttpStatusCode.BadRequest),
            (route, Sample() with { SpeedKmh = -1 }, HttpStatusCode.BadRequest),
            (route, Sample() with { Heading = 361 }, HttpStatusCode.BadRequest),
            (ApiRoutes.SamplesFor("unknown"), Sample(), HttpStatusCode.NotFound)
        };
        foreach (var (path, sample, status) in cases)
        {
            using var response = await client.PostAsJsonAsync(path, sample);
            Assert.Equal(status, response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        }
        var state = await client.GetFromJsonAsync<VehicleSnapshotResponse[]>(ApiRoutes.VehiclesPath);
        Assert.Equal(1, Assert.Single(state!, vehicle => vehicle.Id == "vehicle-1").SampleCount);
    }
}
