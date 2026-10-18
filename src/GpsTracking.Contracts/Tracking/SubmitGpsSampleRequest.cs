using System.ComponentModel.DataAnnotations;

namespace GpsTracking.Contracts.Tracking;

public sealed record SubmitGpsSampleRequest
{
    [Required]
    public DateTimeOffset? Timestamp { get; init; }

    [Required]
    public double? Latitude { get; init; }

    [Required]
    public double? Longitude { get; init; }

    [Required]
    public double? SpeedKmh { get; init; }

    [Required]
    public double? Heading { get; init; }
}
