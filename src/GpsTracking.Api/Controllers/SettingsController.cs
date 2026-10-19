using GpsTracking.Api.Extensions;
using GpsTracking.Api.Realtime;
using GpsTracking.Application.Tracking;
using GpsTracking.Contracts.Tracking;
using Microsoft.AspNetCore.Mvc;

namespace GpsTracking.Api.Controllers;

public sealed class SettingsController(TrackingService tracking, TrackingSession session) : BaseApiController
{
    [HttpGet]
    public IActionResult Get()
        => new GlobalSettingsEvent(session.Id, tracking.GetGlobalSettings().ToResponse())
            .ToOkResult();

    [HttpPut]
    public async Task<IActionResult> Update(UpdateGlobalSettingsRequest request)
        => (await tracking.UpdateGlobalSettingsAsync(request.ToSettings()))
            .ToSettingsResult(this, snapshot => new GlobalSettingsEvent(session.Id, snapshot.ToResponse()));
}
