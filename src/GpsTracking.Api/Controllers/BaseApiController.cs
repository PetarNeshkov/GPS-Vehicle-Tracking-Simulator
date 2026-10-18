using Microsoft.AspNetCore.Mvc;

using GpsTracking.Api.Constants;

namespace GpsTracking.Api.Controllers;

[ApiController]
[Route(ApiConstants.ControllerRouteTemplate)]
public abstract class BaseApiController : ControllerBase
{
}
