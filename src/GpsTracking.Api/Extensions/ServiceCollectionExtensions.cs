using GpsTracking.Api.Configuration;
using GpsTracking.Api.Constants;
using GpsTracking.Application.Abstractions;
using GpsTracking.Application.Tracking;
using GpsTracking.Infrastructure.Storage;
using Microsoft.Extensions.Options;

namespace GpsTracking.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddServiceCollection(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddControllers();
        services.AddOpenApi();
        services.AddProblemDetails();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<ITrackingStore>(provider => new InMemoryTrackingStore(
            provider.GetRequiredService<IOptions<TrackingOptions>>().Value.Vehicles));
        services.AddSingleton<TrackingService>();
        services.AddSingleton<IValidateOptions<TrackingOptions>, TrackingOptionsValidator>();
        services.AddOptions<TrackingOptions>()
            .Bind(configuration.GetRequiredSection(TrackingOptions.SectionName))
            .ValidateOnStart();

        var frontendOrigin = configuration[ApiConstants.FrontendOriginConfigurationKey];
        if (!Uri.TryCreate(frontendOrigin, UriKind.Absolute, out var origin)
            || (origin.Scheme != Uri.UriSchemeHttp && origin.Scheme != Uri.UriSchemeHttps)
            || origin.GetLeftPart(UriPartial.Authority) != frontendOrigin)
        {
            throw new InvalidOperationException(
                $"{ApiConstants.FrontendOriginConfigurationKey} must be an HTTP(S) origin without a path or trailing slash.");
        }

        services.AddCors(options => options.AddPolicy(ApiConstants.FrontendCorsPolicy, policy =>
            policy.WithOrigins(frontendOrigin).AllowAnyHeader().AllowAnyMethod()));

        return services;
    }
}
