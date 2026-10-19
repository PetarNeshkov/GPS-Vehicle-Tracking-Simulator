using GpsTracking.Application.Tracking;
using GpsTracking.Api.Extensions;
using GpsTracking.Api.Realtime;
using GpsTracking.Contracts;
using GpsTracking.Contracts.Tracking;
using Microsoft.AspNetCore.Mvc;

namespace GpsTracking.Api.Controllers;

public sealed class VehiclesController(TrackingService tracking, TrackingSession session) : BaseApiController
{
    [HttpGet]
    [ProducesResponseType<TrackingSnapshotResponse>(StatusCodes.Status200OK)]
    public ActionResult<TrackingSnapshotResponse> Get()
        => new TrackingSnapshotResponse(session.Id,
            tracking.GetSnapshots()
                            .Select(snapshot => snapshot.ToResponse())
                            .ToArray(),
            tracking.GetGlobalSettings().ToResponse())
            .ToOkResult();

    [HttpGet(ApiRoutes.VehicleSettings)]
    public IActionResult GetSettings(string id)
        => tracking.GetVehicle(id).ToVehicleSettingsResult(this);

    [HttpPut(ApiRoutes.VehicleSettings)]
    public async Task<IActionResult> UpdateSettings(string id, UpdateVehicleSettingsRequest request)
        => (await tracking.UpdateSettingsAsync(id, request.ToSettings()))
            .ToSettingsResult(this, snapshot => new VehicleUpdatedEvent(session.Id, snapshot.ToResponse()));

    [HttpPost(ApiRoutes.VehicleSamples)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Submit(string id, SubmitGpsSampleRequest request)
        => (await tracking.SubmitSampleAsync(request.ToCommand(id))).ToActionResult(this, id);
}
