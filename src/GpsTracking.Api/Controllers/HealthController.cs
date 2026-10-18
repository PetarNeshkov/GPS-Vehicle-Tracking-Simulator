using GpsTracking.Api.Constants;
using GpsTracking.Api.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace GpsTracking.Api.Controllers;

public sealed class HealthController : BaseApiController
{
    [HttpGet]
    public IActionResult Get()
    {
        return new { status = ApiConstants.HealthyStatus }.ToOkResult();
    }
}
