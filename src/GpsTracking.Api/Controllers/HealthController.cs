using GpsTracking.Api.Constants;
using Microsoft.AspNetCore.Mvc;

namespace GpsTracking.Api.Controllers;

[ApiController]
[Route(ApiConstants.HealthRoute)]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new { status = "healthy" });
}
