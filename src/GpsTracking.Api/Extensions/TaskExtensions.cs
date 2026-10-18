using Microsoft.AspNetCore.Mvc;

namespace GpsTracking.Api.Extensions;

public static class TaskExtensions
{
    public static OkObjectResult ToOkResult<T>(this T value)
        => new(value);

    public static async Task<OkObjectResult> ToOkResult<T>(this Task<T> task)
        => new(await task);
}
