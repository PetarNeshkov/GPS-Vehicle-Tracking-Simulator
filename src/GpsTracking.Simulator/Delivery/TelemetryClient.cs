using System.Net.Http.Json;
using GpsTracking.Contracts;
using GpsTracking.Contracts.Tracking;

namespace GpsTracking.Simulator.Delivery;

public sealed class TelemetryClient(HttpClient client)
{
    public async Task<SimulationProfile?> GetSimulationAsync(string vehicleId, CancellationToken cancellationToken)
        => await client.GetFromJsonAsync<SimulationProfile>(ApiRoutes.SimulationFor(vehicleId), cancellationToken);

    public async Task SendAsync(string vehicleId, SubmitGpsSampleRequest sample, CancellationToken cancellationToken)
    {
        using var response = await client.PostAsJsonAsync(ApiRoutes.SamplesFor(vehicleId), sample, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
