using System.Net;
using System.Net.Http.Json;
using System.Threading.Channels;
using GpsTracking.Contracts.Tracking;
using GpsTracking.Simulator;
using GpsTracking.Simulator.Configuration;
using GpsTracking.Simulator.Delivery;
using GpsTracking.Tests.Support;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace GpsTracking.Tests;

public sealed class SimulationWorkerTests
{
    [Fact]
    public async Task ShutdownCancelsInFlightProfileRequestsForBothVehicles()
    {
        using var backend = new BlockingBackend();
        using var http = new HttpClient(backend);
        http.BaseAddress = new Uri("http://localhost/");
        using var worker = new SimulationWorker(new TelemetryClient(http), Options.Create(new SimulatorOptions
        {
            VehicleIds = ["vehicle-1", "vehicle-2"]
        }), TimeProvider.System, NullLogger<SimulationWorker>.Instance);
        await worker.StartAsync(CancellationToken.None);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        try
        {
            await backend.Started.Reader.ReadAsync(timeout.Token);
            await backend.Started.Reader.ReadAsync(timeout.Token);
        }
        finally
        {
            await worker.StopAsync(timeout.Token);
        }
        Assert.True(worker.ExecuteTask!.IsCompletedSuccessfully);
        Assert.Equal(2, backend.Cancelled);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OutagesSkipSamplesKeepLastProfileAndRecoverWithoutReplay(bool simulateTimeout)
    {
        var clock = new ReportingTestClock();
        using var handler = new ScriptedBackend(simulateTimeout);
        using var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        using var worker = new SimulationWorker(new TelemetryClient(http), Options.Create(new SimulatorOptions
        {
            VehicleIds = ["vehicle-1", "vehicle-2"], TickIntervalSeconds = 10
        }), clock, NullLogger<SimulationWorker>.Instance);

        await worker.StartAsync(CancellationToken.None);
        try
        {
            var initial = await handler.ReadBothAsync();
            Assert.All(initial, sample => Assert.Equal(110, sample.SpeedKmh));

            handler.Round = 1; // Both profile reads and sample deliveries fail.
            await clock.AdvanceBothVehiclesAsync();
            var failed = await handler.ReadBothAsync();
            Assert.All(failed, sample => Assert.Equal(110, sample.SpeedKmh));

            handler.Round = 2; // Malformed profile, but telemetry delivery recovers.
            await clock.AdvanceBothVehiclesAsync();
            var recovered = await handler.ReadBothAsync();
            Assert.All(recovered, sample =>
            {
                Assert.Equal(110, sample.SpeedKmh);
                Assert.Equal(initial[0].Timestamp!.Value.AddSeconds(20), sample.Timestamp);
            });

            handler.Round = 3;
            await clock.AdvanceBothVehiclesAsync();
            var updated = await handler.ReadBothAsync();
            Assert.All(updated, sample => Assert.Equal(120, sample.SpeedKmh));
            Assert.Equal(8, handler.Attempts);
            Assert.Equal(6, handler.Accepted);
            Assert.False(handler.HasPendingSamples);
        }
        finally
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await worker.StopAsync(timeout.Token);
        }

        Assert.True(worker.ExecuteTask!.IsCompleted);
        Assert.False(worker.ExecuteTask.IsFaulted);
        Assert.Equal(8, handler.Attempts);
    }

    private sealed class BlockingBackend : HttpMessageHandler
    {
        public Channel<bool> Started { get; } = Channel.CreateUnbounded<bool>();
        private int cancelled;
        public int Cancelled => Volatile.Read(ref cancelled);

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Assert.Equal(HttpMethod.Get, request.Method);
            await Started.Writer.WriteAsync(true, cancellationToken);
            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                throw new InvalidOperationException("The blocked request must be cancelled.");
            }
            catch (OperationCanceledException)
            {
                Interlocked.Increment(ref cancelled);
                throw;
            }
        }
    }

    private sealed class ScriptedBackend(bool simulateTimeout) : HttpMessageHandler
    {
        private readonly Channel<SubmitGpsSampleRequest> samples = Channel.CreateUnbounded<SubmitGpsSampleRequest>();
        public int Round;
        private int attempts;
        private int accepted;
        public int Attempts => Volatile.Read(ref attempts);
        public int Accepted => Volatile.Read(ref accepted);
        public bool HasPendingSamples => samples.Reader.TryPeek(out _);

        public async Task<SubmitGpsSampleRequest[]> ReadBothAsync()
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            return [await samples.Reader.ReadAsync(timeout.Token), await samples.Reader.ReadAsync(timeout.Token)];
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var round = Round;
            if (request.Method == HttpMethod.Post)
            {
                var sample = await request.Content!.ReadFromJsonAsync<SubmitGpsSampleRequest>(cancellationToken);
                Interlocked.Increment(ref attempts);
                if (round != 1) Interlocked.Increment(ref accepted);
                await samples.Writer.WriteAsync(sample!, cancellationToken);
                if (round == 1 && simulateTimeout) throw new TaskCanceledException("Simulated HTTP timeout");
                return new HttpResponseMessage(round == 1 ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.NoContent);
            }

            if (round == 1)
            {
                if (simulateTimeout) throw new TaskCanceledException("Simulated HTTP timeout");
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
            if (round == 2)
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("invalid JSON") };

            var profile = SimulationProfile.DefaultFor(0) with { CruisingSpeedKmh = round == 3 ? 120 : 110 };
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(profile) };
        }
    }
}
