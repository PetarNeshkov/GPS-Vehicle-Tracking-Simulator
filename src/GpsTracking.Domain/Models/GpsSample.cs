namespace GpsTracking.Domain.Models;

public sealed record GpsSample
{
    public string VehicleId { get; }
    public DateTimeOffset Timestamp { get; }
    public double Latitude { get; }
    public double Longitude { get; }
    public double SpeedKmh { get; }
    public double Heading { get; }

    public GpsSample(string vehicleId, DateTimeOffset timestamp, double latitude,
        double longitude, double speedKmh, double heading)
    {
        if (string.IsNullOrWhiteSpace(vehicleId))
            throw new ArgumentException("Vehicle ID is required.", nameof(vehicleId));
        if (timestamp == default)
            throw new ArgumentException("A generation timestamp is required.", nameof(timestamp));
        if (!double.IsFinite(latitude) || latitude is < -90 or > 90)
            throw new ArgumentException("Latitude must be between -90 and 90.", nameof(latitude));
        if (!double.IsFinite(longitude) || longitude is < -180 or > 180)
            throw new ArgumentException("Longitude must be between -180 and 180.", nameof(longitude));
        if (!double.IsFinite(speedKmh) || speedKmh < 0)
            throw new ArgumentException("Speed must be finite and nonnegative.", nameof(speedKmh));
        if (!double.IsFinite(heading) || heading is < 0 or > 360)
            throw new ArgumentException("Heading must be between 0 and 360.", nameof(heading));

        VehicleId = vehicleId;
        Timestamp = timestamp.ToUniversalTime();
        Latitude = latitude;
        Longitude = longitude;
        SpeedKmh = speedKmh;
        Heading = heading;
    }
}
