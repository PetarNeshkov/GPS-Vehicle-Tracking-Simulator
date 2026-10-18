using GpsTracking.Domain.Models;

namespace GpsTracking.Tests;

public sealed class GpsSampleTests
{
    [Theory]
    [InlineData(91, 23, 40, 90)]
    [InlineData(42, -181, 40, 90)]
    [InlineData(42, 23, -1, 90)]
    [InlineData(42, 23, 40, 361)]
    [InlineData(double.NaN, 23, 40, 90)]
    [InlineData(42, double.PositiveInfinity, 40, 90)]
    [InlineData(42, 23, double.NaN, 90)]
    [InlineData(42, 23, 40, double.NaN)]
    public void InvalidValuesAreRejected(double latitude, double longitude, double speed, double heading)
    {
        Assert.Throws<ArgumentException>(() =>
            new GpsSample("vehicle-1", DateTimeOffset.UtcNow, latitude, longitude, speed, heading));
    }

    [Fact]
    public void NormalizesUtcWithoutChangingSuppliedSpeedOrHeading()
    {
        var timestamp = new DateTimeOffset(2026, 10, 18, 12, 0, 0, TimeSpan.FromHours(3));
        var sample = new GpsSample("vehicle-1", timestamp, 42, 23, 85.5, 360);
        Assert.Equal(TimeSpan.Zero, sample.Timestamp.Offset);
        Assert.Equal(timestamp, sample.Timestamp);
        Assert.Equal(85.5, sample.SpeedKmh);
        Assert.Equal(360, sample.Heading);
    }

    [Fact]
    public void MissingTimestampIsRejected() => Assert.Throws<ArgumentException>(() =>
        new GpsSample("vehicle-1", default, 42, 23, 40, 0));
}
