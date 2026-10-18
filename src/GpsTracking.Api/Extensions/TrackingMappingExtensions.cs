using GpsTracking.Api.Constants;
using GpsTracking.Application.Tracking;
using GpsTracking.Contracts.Tracking;
using Microsoft.AspNetCore.Mvc;

namespace GpsTracking.Api.Extensions;

public static class TrackingMappingExtensions
{
    // ApiController rejects missing required fields before this mapping runs.
    public static SubmitSampleCommand ToCommand(this SubmitGpsSampleRequest request, string vehicleId)
        => new(vehicleId, request.Timestamp!.Value, request.Latitude!.Value,
            request.Longitude!.Value, request.SpeedKmh!.Value, request.Heading!.Value);

    public static VehicleSnapshotResponse ToResponse(this VehicleSnapshot snapshot)
        => new(snapshot.Id, snapshot.Name,
            snapshot.LatestSample is { } sample
                ? new GpsSampleResponse(sample.Timestamp, sample.Latitude, sample.Longitude,
                    sample.SpeedKmh, sample.Heading)
                : null,
            snapshot.LastReceivedAt, snapshot.SampleCount, snapshot.Revision);

    public static IActionResult ToActionResult(this SubmitSampleResult result,
        ControllerBase controller, string vehicleId)
        => result.Acceptance switch
        {
            SampleAcceptance.Accepted => controller.NoContent(),
            SampleAcceptance.InvalidSample => controller.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: TrackingProblemConstants.InvalidSampleTitle, detail: result.ValidationError),
            SampleAcceptance.UnknownVehicle => controller.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: TrackingProblemConstants.UnknownVehicleTitle,
                detail: string.Format(TrackingProblemConstants.UnknownVehicleDetailFormat, vehicleId)),
            SampleAcceptance.TimestampNotNewer => controller.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: TrackingProblemConstants.TimestampNotNewerTitle,
                detail: TrackingProblemConstants.TimestampNotNewerDetail),
            _ => throw new InvalidOperationException(TrackingProblemConstants.UnexpectedAcceptance)
        };
}
