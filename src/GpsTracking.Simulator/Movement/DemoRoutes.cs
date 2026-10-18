namespace GpsTracking.Simulator.Movement;

public static class DemoRoutes
{
    public static RouteProgressor Create(int vehicleIndex) => vehicleIndex switch
    {
        0 => new RouteProgressor(42,
            new(42.6970, 23.3120), new(42.6970, 23.3330),
            new(42.7005, 23.3330), new(42.7005, 23.3120)),
        1 => new RouteProgressor(66,
            new(42.6985, 23.3320), new(42.6985, 23.3130),
            new(42.6945, 23.3130), new(42.6945, 23.3320)),
        _ => throw new ArgumentOutOfRangeException(nameof(vehicleIndex))
    };
}
