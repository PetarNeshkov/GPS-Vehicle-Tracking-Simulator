namespace GpsTracking.Api.Constants;

public static class ApiConstants
{
    public const string FrontendCorsPolicy = "Frontend";
    public const string FrontendOriginConfigurationKey = "Frontend:Origin";
    public const string InvalidFrontendOrigin = FrontendOriginConfigurationKey
        + " must be an HTTP(S) origin without a path or trailing slash.";
    public const string ControllerRouteTemplate = "api/[controller]";
    public const string HealthRoute = "api/health";
    public const string HealthyStatus = "healthy";
}
