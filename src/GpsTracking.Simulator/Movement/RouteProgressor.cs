namespace GpsTracking.Simulator.Movement;

public sealed class RouteProgressor
{
    private const double EarthRadiusMeters = 6_371_000;
    private readonly RoutePoint[] points;
    private readonly double[] lengths;
    private readonly double speedKmh;
    private readonly double totalLength;
    private int segment;
    private double segmentDistance;

    public RouteProgressor(double speedKmh, params RoutePoint[] points)
    {
        if (!double.IsFinite(speedKmh) || speedKmh <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(speedKmh));
        }

        if (points.Length < 2 || points.Any(point => point is null
            || !double.IsFinite(point.Latitude) || point.Latitude is < -90 or > 90
            || !double.IsFinite(point.Longitude) || point.Longitude is < -180 or > 180))
        {
            throw new ArgumentException("A route needs at least two valid points.", nameof(points));
        }

        this.points = points.ToArray();
        this.speedKmh = speedKmh;
        lengths = Enumerable.Range(0, points.Length)
            .Select(index => Distance(points[index], points[(index + 1) % points.Length])).ToArray();

        if (lengths.Any(length => length < 0.01))
        {
            throw new ArgumentException("Adjacent route points must be distinct, including the closing edge.",
                nameof(points));
        }

        totalLength = lengths.Sum();
    }

    public RoutePosition Advance(TimeSpan elapsed)
    {
        if (elapsed < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(elapsed));
        }

        // Full laps leave the same position; keep long scheduling delays bounded.
        var remaining = (speedKmh / 3.6 * elapsed.TotalSeconds) % totalLength;
        while (remaining >= lengths[segment] - segmentDistance)
        {
            remaining -= lengths[segment] - segmentDistance;
            segment = (segment + 1) % points.Length;
            segmentDistance = 0;
        }
        segmentDistance += remaining;

        var from = points[segment];
        var to = points[(segment + 1) % points.Length];
        var fraction = segmentDistance / lengths[segment];
        var heading = Math.Atan2(
            (to.Longitude - from.Longitude) * Math.Cos(Radians((from.Latitude + to.Latitude) / 2)),
            to.Latitude - from.Latitude) * 180 / Math.PI;

        return new RoutePosition(
            from.Latitude + (to.Latitude - from.Latitude) * fraction,
            from.Longitude + (to.Longitude - from.Longitude) * fraction,
            speedKmh, (heading + 360) % 360);
    }

    private static double Distance(RoutePoint from, RoutePoint to)
    {
        var a = Math.Pow(Math.Sin(Radians(to.Latitude - from.Latitude) / 2), 2)
            + Math.Cos(Radians(from.Latitude)) * Math.Cos(Radians(to.Latitude))
            * Math.Pow(Math.Sin(Radians(to.Longitude - from.Longitude) / 2), 2);
        return 2 * EarthRadiusMeters * Math.Asin(Math.Sqrt(Math.Clamp(a, 0, 1)));
    }

    private static double Radians(double degrees) => degrees * Math.PI / 180;
}
