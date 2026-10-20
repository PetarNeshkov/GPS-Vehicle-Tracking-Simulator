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
    TimeProvider clock, ILogger<SimulationWorker> logger)
    : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
        => Task.WhenAll(options.Value.VehicleIds.Select((id, index)
            => RunVehicleAsync(id, DemoScenarios.Create(index), stoppingToken)));

    private async Task RunVehicleAsync(string vehicleId, ScenarioProgressor scenario, CancellationToken stoppingToken)
    {
        var previousTick = clock.GetTimestamp();
        ScenarioPhaseKind? previousPhase = null;
        while (!stoppingToken.IsCancellationRequested)
        {
            SimulationProfile? profile = null;
            try
            {
                profile = await client.GetSimulationAsync(vehicleId, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception) when (exception is HttpRequestException or OperationCanceledException
                or System.Text.Json.JsonException)
            {
                logger.LogWarning(exception, SimulatorConstants.ProfileUnavailable, vehicleId);
            }

            var tick = clock.GetTimestamp();
            var progress = scenario.Advance(clock.GetElapsedTime(previousTick, tick));
            previousTick = tick;

            if (profile is not null)
            {
                try
                {
                    scenario.ApplyProfile(profile);
                    progress = scenario.Advance(TimeSpan.Zero);
                }
                catch (ArgumentException exception)
                {
                    logger.LogWarning(exception, SimulatorConstants.ProfileUnavailable, vehicleId);
                }
            }

            var position = progress.Position;

            if (progress.Phase != previousPhase)
            {
                logger.LogInformation(SimulatorConstants.PhaseChanged, vehicleId, progress.Phase, position.SpeedKmh);
                previousPhase = progress.Phase;
            }

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
                logger.LogWarning(exception, SimulatorConstants.DeliveryFailed,
                    vehicleId, sample.Timestamp);
            }

            // Waiting after delivery prevents catch-up bursts, even after a timeout.
            await Task.Delay(TimeSpan.FromSeconds(options.Value.TickIntervalSeconds), clock, stoppingToken);
        }
    }
}
