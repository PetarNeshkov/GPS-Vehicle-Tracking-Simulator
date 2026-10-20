namespace GpsTracking.Domain.Rules;

public static class GeographicDistance
{
    private const double EarthRadiusMeters = 6_371_000;
    private const double DegreesToRadians = Math.PI / 180;

    public static double Meters(double latitude1, double longitude1, double latitude2, double longitude2)
    {
        var latitudeDelta = (latitude2 - latitude1) * DegreesToRadians;
        var longitudeDelta = (longitude2 - longitude1) * DegreesToRadians;
        var haversine = Math.Pow(Math.Sin(latitudeDelta / 2), 2)
            + Math.Cos(latitude1 * DegreesToRadians) * Math.Cos(latitude2 * DegreesToRadians)
            * Math.Pow(Math.Sin(longitudeDelta / 2), 2);

        return 2 * EarthRadiusMeters * Math.Asin(Math.Sqrt(Math.Clamp(haversine, 0, 1)));
    }
}
