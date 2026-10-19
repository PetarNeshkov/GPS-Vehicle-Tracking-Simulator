using GpsTracking.Api.Extensions;
using GpsTracking.Api.Realtime;
using GpsTracking.Application.Tracking.Queries;
using GpsTracking.Contracts.Tracking;
using Microsoft.AspNetCore.Mvc;

namespace GpsTracking.Api.Controllers;

public sealed class GeofencesController(GetGeofencesHandler geofences, TrackingSession session)
    : BaseApiController
{
    [HttpGet]
    [ProducesResponseType<GeofencesResponse>(StatusCodes.Status200OK)]
    public IActionResult Get()
        => new GeofencesResponse(session.Id, geofences.Handle()
            .Select(geofence => geofence.ToResponse()).ToArray())
            .ToOkResult();
}
