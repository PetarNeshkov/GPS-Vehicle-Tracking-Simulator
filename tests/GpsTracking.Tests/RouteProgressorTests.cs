using GpsTracking.Simulator.Movement;

namespace GpsTracking.Tests;

public sealed class RouteProgressorTests
{
    private static RouteProgressor CreateRoute() => new(36, new(0, 0), new(0, 0.001));

    [Fact]
    public void DistanceFollowsSpeedAndElapsedTime()
    {
        var position = CreateRoute().Advance(TimeSpan.FromSeconds(5));
        Assert.InRange(position.Longitude, 0.000449, 0.000451); // 50 metres east.
        Assert.Equal(0, position.Latitude);
        Assert.Equal(36, position.SpeedKmh);
        Assert.Equal(90, position.Heading);
    }

    [Fact]
    public void OvershootContinuesAlongTheClosingEdge()
    {
        var position = CreateRoute().Advance(TimeSpan.FromSeconds(12));
        Assert.InRange(position.Longitude, 0.00091, 0.00093);
        Assert.Equal(270, position.Heading);
    }

    [Fact]
    public void LongAdvanceMatchesManySmallAdvancesAcrossMultipleLaps()
    {
        var expected = CreateRoute().Advance(TimeSpan.FromSeconds(100));
        var route = CreateRoute();
        RoutePosition actual = route.Advance(TimeSpan.Zero);
        for (var i = 0; i < 100; i++) actual = route.Advance(TimeSpan.FromSeconds(1));
        Assert.Equal(expected.Longitude, actual.Longitude, 10);
        Assert.Equal(expected.Heading, actual.Heading);
    }

    [Fact]
    public void VehicleProgressIsIndependent()
    {
        var first = DemoRoutes.Create(0);
        var second = DemoRoutes.Create(1);
        var before = second.Advance(TimeSpan.Zero);
        first.Advance(TimeSpan.FromSeconds(100));
        Assert.Equal(before, second.Advance(TimeSpan.Zero));
    }
}
