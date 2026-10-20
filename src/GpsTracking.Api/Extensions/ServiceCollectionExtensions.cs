using GpsTracking.Api.Configuration;
using GpsTracking.Api.Constants;
using GpsTracking.Api.Realtime;
using GpsTracking.Application.Abstractions;
using GpsTracking.Application.Simulation;
using GpsTracking.Contracts.Tracking;
using GpsTracking.Application.Tracking.Commands;
using GpsTracking.Application.Tracking.Queries;
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
        services.AddSignalR();
        services.AddSingleton<TrackingSession>();
        services.AddSingleton<ISimulationSettingsStore>(provider =>
        {
            var vehicles = provider.GetRequiredService<IOptions<TrackingOptions>>().Value.Vehicles;
            var seeds = vehicles.Select((vehicle, index) => new
            {
                vehicle.Id,
                Settings = SimulationProfile.DefaultFor(index).ToSettings()
            }).ToDictionary(vehicle => vehicle.Id, vehicle => vehicle.Settings);

            return new InMemorySimulationSettingsStore(seeds);
        });
        services.AddSingleton<GetSimulationSettingsHandler>();
        services.AddSingleton<UpdateSimulationSettingsHandler>();
        services.AddSingleton<ITrackingPublisher, SignalRTrackingPublisher>();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<ITrackingStore>(provider => new InMemoryTrackingStore(
            provider.GetRequiredService<IOptions<TrackingOptions>>().Value.Vehicles,
            provider.GetRequiredService<IOptions<TrackingOptions>>().Value.GlobalSettings,
            provider.GetRequiredService<IOptions<TrackingOptions>>().Value.Geofences));
        services.AddSingleton<SubmitSampleHandler>();
        services.AddSingleton<GetSnapshotsHandler>();
        services.AddSingleton<GetHistoryHandler>();
        services.AddSingleton<GetSegmentsHandler>();
        services.AddSingleton<GetTrackingStateHandler>();
        services.AddSingleton<GetVehicleSettingsHandler>();
        services.AddSingleton<GetAlertsHandler>();
        services.AddSingleton<GetGeofencesHandler>();
        services.AddSingleton<GetGlobalSettingsHandler>();
        services.AddSingleton<UpdateVehicleSettingsHandler>();
        services.AddSingleton<UpdateGlobalSettingsHandler>();
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
            policy.WithOrigins(frontendOrigin).AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

        return services;
    }
}
