using GpsTracking.Api.Extensions;
using GpsTracking.Application.Abstractions;
using GpsTracking.Application.Tracking;
using GpsTracking.Contracts.Tracking;
using Microsoft.AspNetCore.SignalR;

namespace GpsTracking.Api.Realtime;

public sealed class SignalRTrackingPublisher(IHubContext<TrackingHub> hub, TrackingSession session, ILogger<SignalRTrackingPublisher> logger)
    : ITrackingPublisher
{
    private const string PublicationFailed = "Live publication failed for {VehicleId} revision {Revision}; telemetry remains stored";
    private static readonly TimeSpan PublicationTimeout = TimeSpan.FromSeconds(5);

    public async Task PublishAsync(VehicleSnapshot snapshot)
    {
        using var timeout = new CancellationTokenSource(PublicationTimeout);
        try
        {
            await hub.Clients.All.SendAsync(TrackingHubContract.VehicleUpdated,
                new VehicleUpdatedEvent(session.Id, snapshot.ToResponse()), timeout.Token);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, PublicationFailed, snapshot.Id, snapshot.Revision);
        }
    }
}
