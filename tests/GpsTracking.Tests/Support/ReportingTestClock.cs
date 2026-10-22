using System.Threading.Channels;

namespace GpsTracking.Tests.Support;

// A narrowly scoped clock for the worker's one-shot reporting delays.
// Tests wait for BOTH loops to schedule their delay before advancing time.
internal sealed class ReportingTestClock : TimeProvider
{
    private readonly Channel<ReportingTimer> timers = Channel.CreateUnbounded<ReportingTimer>();
    private readonly DateTimeOffset start = new(2026, 10, 22, 12, 0, 0, TimeSpan.Zero);
    private long elapsedTicks;

    public override long TimestampFrequency => TimeSpan.TicksPerSecond;
    public override long GetTimestamp() => Interlocked.Read(ref elapsedTicks);
    public override DateTimeOffset GetUtcNow() => start.AddTicks(GetTimestamp());

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        Assert.Equal(Timeout.InfiniteTimeSpan, period);
        var timer = new ReportingTimer(callback, state, dueTime);
        Assert.True(timers.Writer.TryWrite(timer));

        return timer;
    }

    public async Task AdvanceBothVehiclesAsync()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var first = await timers.Reader.ReadAsync(timeout.Token);
        var second = await timers.Reader.ReadAsync(timeout.Token);

        Assert.Equal(TimeSpan.FromSeconds(10), first.DueTime);
        Assert.Equal(first.DueTime, second.DueTime);
        Interlocked.Add(ref elapsedTicks, first.DueTime.Ticks);
        first.Fire();
        second.Fire();
    }

    private sealed class ReportingTimer(TimerCallback callback, object? state, TimeSpan dueTime) : ITimer
    {
        private int disposed;

        public TimeSpan DueTime { get; } = dueTime;

        public void Fire()
        {
            if (Interlocked.Exchange(ref disposed, 1) == 0) callback(state);
        }

        public bool Change(TimeSpan dueTime, TimeSpan period) => throw new NotSupportedException();

        public void Dispose() => Interlocked.Exchange(ref disposed, 1);

        public ValueTask DisposeAsync()
        {
            Dispose(); return ValueTask.CompletedTask;
        }
    }
}
