using GpsTracking.Application.Abstractions;
using GpsTracking.Domain.Models;

namespace GpsTracking.Application.Tracking.Commands;

public sealed class SubmitSampleHandler(ITrackingStore store, TimeProvider timeProvider, ITrackingPublisher publisher)
{
    public async Task<SubmitSampleResult> HandleAsync(SubmitSampleCommand command)
    {
        GpsSample sample;
        try
        {
            sample = new GpsSample(command.VehicleId, command.Timestamp, command.Latitude,
                command.Longitude, command.SpeedKmh, command.Heading);
        }
        catch (ArgumentException exception)
        {
            return new SubmitSampleResult(SampleAcceptance.InvalidSample, exception.Message);
        }

        var result = store.AppendWithSnapshot(sample, timeProvider.GetUtcNow());
        if (result.Snapshot is { } snapshot)
        {
            await publisher.PublishAsync(snapshot);
        }

        return new SubmitSampleResult(result.Acceptance);
    }
}
