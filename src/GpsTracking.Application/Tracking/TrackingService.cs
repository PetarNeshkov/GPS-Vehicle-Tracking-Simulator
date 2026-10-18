using GpsTracking.Application.Abstractions;
using GpsTracking.Domain.Models;

namespace GpsTracking.Application.Tracking;

public sealed class TrackingService(ITrackingStore store, TimeProvider timeProvider)
{
    public SubmitSampleResult SubmitSample(SubmitSampleCommand command)
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

        return new SubmitSampleResult(ProcessSample(sample));
    }

    public SampleAcceptance ProcessSample(GpsSample sample)
        => store.Append(sample, timeProvider.GetUtcNow());

    public IReadOnlyList<VehicleSnapshot> GetSnapshots()
        => store.GetSnapshots();
}
