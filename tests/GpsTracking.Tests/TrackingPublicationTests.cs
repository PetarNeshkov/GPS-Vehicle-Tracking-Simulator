using GpsTracking.Api.Realtime;
using GpsTracking.Application.Tracking;
using GpsTracking.Application.Tracking.Commands;
using GpsTracking.Contracts.Tracking;
using GpsTracking.Domain.Models;
using GpsTracking.Infrastructure.Storage;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;

namespace GpsTracking.Tests;

public sealed class TrackingPublicationTests
{
    [Fact]
    public async Task PublicationRunsOutsideVehicleLockAndFailureDoesNotUndoAcceptance()
    {
        var store = new InMemoryTrackingStore([new VehicleDefinition { Id = "vehicle-1", Name = "First" }]);
        var client = new RecordingClient();
        var session = new TrackingSession();
        var publisher = new SignalRTrackingPublisher(new TestHubContext(client), session,
            NullLogger<SignalRTrackingPublisher>.Instance);
        var service = new SubmitSampleHandler(store, TimeProvider.System, publisher);
        var command = new SubmitSampleCommand("vehicle-1", DateTimeOffset.UtcNow, 42, 23, 85, 360);

        client.BeforeSend = async () =>
        {
            // A different thread must be able to read while publication is pending.
            var snapshots = await Task.Run(store.GetSnapshots).WaitAsync(TimeSpan.FromSeconds(2));
            Assert.Equal(1, snapshots[0].Revision);
        };
        Assert.Equal(SampleAcceptance.Accepted, (await service.HandleAsync(command)).Acceptance);
        var published = Assert.IsType<VehicleUpdatedEvent>(client.Payload);

        Assert.Equal(session.Id, published.SessionId);
        Assert.Equal(1, published.Vehicle.Revision);
        Assert.Equal(TrackingHubContract.VehicleUpdated, client.Method);
        Assert.Equal(SampleAcceptance.TimestampNotNewer, (await service.HandleAsync(command)).Acceptance);
        Assert.Equal(1, client.Calls);

        client.BeforeSend = () => throw new InvalidOperationException("Simulated transport failure");
        Assert.Equal(SampleAcceptance.Accepted,
            (await service.HandleAsync(command with { Timestamp = command.Timestamp.AddSeconds(10) })).Acceptance);
        Assert.Equal(2, store.GetSnapshots()[0].Revision);

        var changedGlobal = await new UpdateGlobalSettingsHandler(store, publisher).HandleAsync(new GlobalSettings
        {
            StopRadiusMeters = 20
        });
        Assert.Equal(20, changedGlobal.Value!.Settings.StopRadiusMeters);
        Assert.Equal(20, store.GetGlobalSettings().Settings.StopRadiusMeters);
        Assert.Equal(1, published.Vehicle.Revision);
        Assert.NotEqual(session.Id, new TrackingSession().Id);
    }

    private sealed class RecordingClient : IClientProxy
    {
        public Func<Task> BeforeSend { get; set; } = () => Task.CompletedTask;
        public object? Payload { get; private set; }
        public string? Method { get; private set; }
        public int Calls { get; private set; }
        public async Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default)
        {
            Calls++;
            await BeforeSend();
            Method = method;
            Payload = Assert.Single(args);
        }
    }

    private sealed class TestHubContext(IClientProxy client) : IHubContext<TrackingHub>
    {
        public IHubClients Clients { get; } = new TestClients(client);
        public IGroupManager Groups => throw new NotSupportedException();
    }

    private sealed class TestClients(IClientProxy client) : IHubClients
    {
        public IClientProxy All => client;
        public IClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds) => throw new NotSupportedException();
        public IClientProxy Client(string connectionId) => throw new NotSupportedException();
        public IClientProxy Clients(IReadOnlyList<string> connectionIds) => throw new NotSupportedException();
        public IClientProxy Group(string groupName) => throw new NotSupportedException();
        public IClientProxy GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => throw new NotSupportedException();
        public IClientProxy Groups(IReadOnlyList<string> groupNames) => throw new NotSupportedException();
        public IClientProxy User(string userId) => throw new NotSupportedException();
        public IClientProxy Users(IReadOnlyList<string> userIds) => throw new NotSupportedException();
    }
}
