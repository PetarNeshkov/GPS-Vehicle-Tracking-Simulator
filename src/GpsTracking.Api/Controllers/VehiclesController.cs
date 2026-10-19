using GpsTracking.Application.Tracking.Commands;
using GpsTracking.Application.Tracking.Queries;
using GpsTracking.Api.Extensions;
using GpsTracking.Api.Realtime;
using GpsTracking.Contracts;
using GpsTracking.Contracts.Tracking;
using Microsoft.AspNetCore.Mvc;

namespace GpsTracking.Api.Controllers;

public sealed class VehiclesController(GetSnapshotsHandler snapshots, GetVehicleSettingsHandler vehicleSettings,
    GetGlobalSettingsHandler globalSettings, UpdateVehicleSettingsHandler updateSettings,
    SubmitSampleHandler submitSample, GetGeofencesHandler geofences, TrackingSession session) : BaseApiController
{
    [HttpGet]
    [ProducesResponseType<TrackingSnapshotResponse>(StatusCodes.Status200OK)]
    public ActionResult<TrackingSnapshotResponse> Get()
        => new TrackingSnapshotResponse(session.Id,
            snapshots.Handle().Select(snapshot => snapshot.ToResponse()).ToArray(),
            globalSettings.Handle().ToResponse(),
            geofences.Handle().Select(geofence => geofence.ToResponse()).ToArray())
            .ToOkResult();

    [HttpGet(ApiRoutes.VehicleSettings)]
    public IActionResult GetSettings(string id)
        => vehicleSettings.Handle(id).ToVehicleSettingsResult(this);

    [HttpPut(ApiRoutes.VehicleSettings)]
    public async Task<IActionResult> UpdateSettings(string id, UpdateVehicleSettingsRequest request)
        => (await updateSettings.HandleAsync(id, request.ToSettings()))
            .ToSettingsResult(this, snapshot => new VehicleUpdatedEvent(session.Id, snapshot.ToResponse()));

    [HttpPost(ApiRoutes.VehicleSamples)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Submit(string id, SubmitGpsSampleRequest request)
        => (await submitSample.HandleAsync(request.ToCommand(id))).ToActionResult(this, id);
}
