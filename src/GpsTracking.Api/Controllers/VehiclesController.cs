using GpsTracking.Application.Tracking;
using GpsTracking.Api.Extensions;
using GpsTracking.Contracts;
using GpsTracking.Contracts.Tracking;
using Microsoft.AspNetCore.Mvc;

namespace GpsTracking.Api.Controllers;

public sealed class VehiclesController(TrackingService tracking) : BaseApiController
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<VehicleSnapshotResponse>>(StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<VehicleSnapshotResponse>> Get()
        => tracking.GetSnapshots().Select(snapshot => snapshot.ToResponse()).ToArray().ToOkResult();

    [HttpPost(ApiRoutes.VehicleSamples)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public IActionResult Submit(string id, SubmitGpsSampleRequest request)
        => tracking.SubmitSample(request.ToCommand(id)).ToActionResult(this, id);
}
