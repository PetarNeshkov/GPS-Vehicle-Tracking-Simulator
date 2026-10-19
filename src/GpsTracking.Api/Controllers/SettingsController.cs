using GpsTracking.Api.Extensions;
using GpsTracking.Api.Realtime;
using GpsTracking.Application.Tracking.Commands;
using GpsTracking.Application.Tracking.Queries;
using GpsTracking.Contracts.Tracking;
using Microsoft.AspNetCore.Mvc;

namespace GpsTracking.Api.Controllers;

public sealed class SettingsController(GetGlobalSettingsHandler settings, UpdateGlobalSettingsHandler updateSettings, TrackingSession session) : BaseApiController
{
    [HttpGet]
    public IActionResult Get()
        => new GlobalSettingsEvent(session.Id, settings.Handle().ToResponse())
            .ToOkResult();

    [HttpPut]
    public async Task<IActionResult> Update(UpdateGlobalSettingsRequest request)
        => (await updateSettings.HandleAsync(request.ToSettings()))
            .ToSettingsResult(this, snapshot => new GlobalSettingsEvent(session.Id, snapshot.ToResponse()));
}
