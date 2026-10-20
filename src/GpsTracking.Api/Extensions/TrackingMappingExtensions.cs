using GpsTracking.Api.Constants;
using GpsTracking.Application.Tracking;
using GpsTracking.Contracts.Tracking;
using GpsTracking.Domain.Models;
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
            snapshot.LastReceivedAt, snapshot.SampleCount, snapshot.Revision,
            (snapshot.Settings ?? new VehicleSettings()).ToResponse(),
            (snapshot.Alerts ?? []).Select(alert => alert.ToResponse()).ToArray(),
            (snapshot.GeofenceMemberships ?? []).Select(membership =>
                new GeofenceMembershipResponse(membership.GeofenceId, membership.State.ToString())).ToArray(),
            snapshot.Movement is { } movement
                ? new MovementResponse(movement.State.ToString(), movement.StopStartedAt, movement.StopElapsedSeconds) : null,
            (snapshot.Segments ?? []).Select(segment => segment.ToResponse()).ToArray());

    public static GpsSampleResponse ToResponse(this GpsSample sample)
        => new(sample.Timestamp, sample.Latitude, sample.Longitude, sample.SpeedKmh, sample.Heading);

    public static RouteSegmentResponse ToResponse(this RouteSegment segment)
        => new(segment.Id, segment.StartedAt, segment.EndedAt, segment.Points.Select(point => point.ToResponse()).ToArray());

    public static GlobalSettingsEvent ToEvent(this GlobalSettingsSnapshot snapshot, Guid sessionId)
        => new(sessionId, snapshot.ToResponse(), snapshot.Vehicles?.Select(vehicle => vehicle.ToResponse()).ToArray());

    public static TrackingSnapshotResponse ToResponse(this TrackingState state, Guid sessionId,
        IReadOnlyList<GeofenceDefinition> geofences)
        => new(sessionId, state.Vehicles.Select(vehicle => vehicle.ToResponse()).ToArray(),
            state.GlobalSettings.ToResponse(), geofences.Select(zone => zone.ToResponse()).ToArray());

    public static IActionResult ToHistoryResult(this HistorySnapshot? history, ControllerBase controller, Guid sessionId)
        => history is null
            ? controller.Problem(statusCode: StatusCodes.Status404NotFound, title: TrackingProblemConstants.UnknownVehicleTitle)
            : new HistoryResponse(sessionId, history.VehicleId, history.Revision,
                history.Samples.Select(sample => sample.ToResponse()).ToArray()).ToOkResult();

    public static IActionResult ToSegmentsResult(this VehicleSnapshot? snapshot, ControllerBase controller, Guid sessionId)
        => snapshot is null
            ? controller.Problem(statusCode: StatusCodes.Status404NotFound, title: TrackingProblemConstants.UnknownVehicleTitle)
            : new SegmentsResponse(sessionId, snapshot.Id, snapshot.Revision,
                (snapshot.Segments ?? []).Select(segment => segment.ToResponse()).ToArray()).ToOkResult();

    public static GeofenceResponse ToResponse(this GeofenceDefinition definition)
        => new(definition.Id, definition.Name, definition.MinLatitude, definition.MaxLatitude,
            definition.MinLongitude, definition.MaxLongitude);

    public static VehicleSettingsResponse ToResponse(this VehicleSettings settings)
        => new(settings.SpeedLimitKmh, settings.SpeedingDurationSeconds, settings.StopDurationSeconds);

    public static GlobalSettingsResponse ToResponse(this GlobalSettingsSnapshot snapshot)
        => new(snapshot.Settings.StopRadiusMeters, snapshot.Revision);

    public static AlertResponse ToResponse(this TrackingAlert alert)
        => new(alert.Id, alert.VehicleId, alert.Type, alert.OccurredAt, alert.CreatedAt,
            alert.SpeedKmh, alert.SpeedLimitKmh, alert.SpeedingDurationSeconds, alert.GeofenceId, alert.GeofenceName);

    public static VehicleSettings ToSettings(this UpdateVehicleSettingsRequest request)
        => new() { SpeedLimitKmh = request.SpeedLimitKmh!.Value,
            SpeedingDurationSeconds = request.SpeedingDurationSeconds!.Value,
            StopDurationSeconds = request.StopDurationSeconds!.Value };

    public static GlobalSettings ToSettings(this UpdateGlobalSettingsRequest request)
        => new() { StopRadiusMeters = request.StopRadiusMeters!.Value };

    public static IActionResult ToSettingsResult<T>(this SettingsUpdateResult<T> result,
        ControllerBase controller, Func<T, object> map) where T : class
    {
        if (result.NotFound)
            return controller.Problem(statusCode: StatusCodes.Status404NotFound,
                title: TrackingProblemConstants.UnknownVehicleTitle);
        if (result.Error is not null)
            return controller.Problem(statusCode: StatusCodes.Status400BadRequest,
                title: TrackingProblemConstants.InvalidSettingsTitle, detail: result.Error);
        return map(result.Value!).ToOkResult();
    }

    public static IActionResult ToVehicleSettingsResult(this VehicleSnapshot? snapshot, ControllerBase controller)
        => snapshot is null
            ? controller.Problem(statusCode: StatusCodes.Status404NotFound, title: TrackingProblemConstants.UnknownVehicleTitle)
            : snapshot.ToResponse().ToOkResult();

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
