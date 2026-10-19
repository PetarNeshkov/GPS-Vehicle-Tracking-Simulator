using GpsTracking.Api.Extensions;
using GpsTracking.Api.Realtime;
using GpsTracking.Application.Tracking;
using GpsTracking.Contracts.Tracking;
using Microsoft.AspNetCore.Mvc;

namespace GpsTracking.Api.Controllers;

public sealed class AlertsController(TrackingService tracking, TrackingSession session) : BaseApiController
{
    [HttpGet]
    public IActionResult Get()
        => new AlertsResponse(session.Id, tracking.GetAlerts()
                                                        .Select(alert => alert.ToResponse())
                                                        .ToArray())
            .ToOkResult();
}
