using GpsTracking.Contracts.Tracking;
using GpsTracking.Simulator.Configuration;
using GpsTracking.Simulator.Delivery;
using GpsTracking.Simulator.Movement;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GpsTracking.Simulator;

public sealed class SimulationWorker(
    TelemetryClient client, IOptions<SimulatorOptions> options,
    TimeProvider clock, ILogger<SimulationWorker> logger) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
        => Task.WhenAll(options.Value.VehicleIds.Select((id, index)
            => RunVehicleAsync(id, DemoRoutes.Create(index), stoppingToken)));

    private async Task RunVehicleAsync(string vehicleId, RouteProgressor route, CancellationToken stoppingToken)
    {
        var previousTick = clock.GetTimestamp();
        while (!stoppingToken.IsCancellationRequested)
        {
            var tick = clock.GetTimestamp();
            var position = route.Advance(clock.GetElapsedTime(previousTick, tick));
            previousTick = tick;
            var sample = new SubmitGpsSampleRequest
            {
                Timestamp = clock.GetUtcNow(),
                Latitude = position.Latitude,
                Longitude = position.Longitude,
                SpeedKmh = position.SpeedKmh,
                Heading = position.Heading
            };

            try
            {
                await client.SendAsync(vehicleId, sample, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception) when (exception is HttpRequestException or OperationCanceledException)
            {
                logger.LogWarning(exception, "Delivery failed for {VehicleId} at {GpsTimestamp}; continuing next interval",
                    vehicleId, sample.Timestamp);
            }

            // Waiting after delivery prevents catch-up bursts, even after a timeout.
            await Task.Delay(TimeSpan.FromSeconds(options.Value.TickIntervalSeconds), clock, stoppingToken);
        }
    }
}
