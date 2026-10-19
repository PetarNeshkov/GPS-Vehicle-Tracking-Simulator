using GpsTracking.Api.Extensions;
using GpsTracking.Api.Realtime;
using GpsTracking.Application.Tracking.Queries;
using GpsTracking.Contracts.Tracking;
using Microsoft.AspNetCore.Mvc;

namespace GpsTracking.Api.Controllers;

public sealed class AlertsController(GetAlertsHandler alerts, TrackingSession session) : BaseApiController
{
    [HttpGet]
    public IActionResult Get()
        => new AlertsResponse(session.Id, alerts.Handle()
            .Select(alert => alert.ToResponse()).ToArray())
            .ToOkResult();
}
