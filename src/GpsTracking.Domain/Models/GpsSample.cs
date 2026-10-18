using GpsTracking.Domain.Constants;

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
            throw new ArgumentException(GpsSampleConstants.VehicleIdRequired, nameof(vehicleId));
        if (timestamp == default)
            throw new ArgumentException(GpsSampleConstants.TimestampRequired, nameof(timestamp));
        if (!double.IsFinite(latitude) || latitude is < GpsSampleConstants.MinimumLatitude or > GpsSampleConstants.MaximumLatitude)
            throw new ArgumentException(GpsSampleConstants.InvalidLatitude, nameof(latitude));
        if (!double.IsFinite(longitude) || longitude is < GpsSampleConstants.MinimumLongitude or > GpsSampleConstants.MaximumLongitude)
            throw new ArgumentException(GpsSampleConstants.InvalidLongitude, nameof(longitude));
        if (!double.IsFinite(speedKmh) || speedKmh < GpsSampleConstants.MinimumSpeedKmh)
            throw new ArgumentException(GpsSampleConstants.InvalidSpeed, nameof(speedKmh));
        if (!double.IsFinite(heading) || heading is < GpsSampleConstants.MinimumHeading or > GpsSampleConstants.MaximumHeading)
            throw new ArgumentException(GpsSampleConstants.InvalidHeading, nameof(heading));

        VehicleId = vehicleId;
        Timestamp = timestamp.ToUniversalTime();
        Latitude = latitude;
        Longitude = longitude;
        SpeedKmh = speedKmh;
        Heading = heading;
    }
}
