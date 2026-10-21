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

        AddTrackingRealtime(services);

        services.AddSingleton(TimeProvider.System);

        AddTrackingStorage(services);

        AddTrackingHandlers(services);

        AddTrackingOptions(services, configuration);

        AddFrontendCors(services, configuration);

        return services;
    }

    private static void AddTrackingRealtime(IServiceCollection services)
    {
        services.AddSignalR();
        services.AddSingleton<TrackingSession>();
        services.AddSingleton<ITrackingPublisher, SignalRTrackingPublisher>();
    }

    private static void AddTrackingStorage(IServiceCollection services)
    {
        services.AddSingleton<ITrackingStore>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<TrackingOptions>>().Value;

            return new InMemoryTrackingStore(options.Vehicles, options.GlobalSettings, options.Geofences);
        });
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
    }

    private static void AddTrackingHandlers(IServiceCollection services)
    {
        services.AddScoped<GetSimulationSettingsHandler>();
        services.AddScoped<UpdateSimulationSettingsHandler>();
        services.AddScoped<SubmitSampleHandler>();
        services.AddScoped<GetSnapshotsHandler>();
        services.AddScoped<GetHistoryHandler>();
        services.AddScoped<GetSegmentsHandler>();
        services.AddScoped<GetTrackingStateHandler>();
        services.AddScoped<GetVehicleSettingsHandler>();
        services.AddScoped<GetAlertsHandler>();
        services.AddScoped<GetGeofencesHandler>();
        services.AddScoped<GetGlobalSettingsHandler>();
        services.AddScoped<UpdateVehicleSettingsHandler>();
        services.AddScoped<UpdateGlobalSettingsHandler>();
    }

    private static void AddTrackingOptions(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IValidateOptions<TrackingOptions>, TrackingOptionsValidator>();
        services.AddOptions<TrackingOptions>()
            .Bind(configuration.GetRequiredSection(TrackingOptions.SectionName))
            .ValidateOnStart();
    }

    private static void AddFrontendCors(IServiceCollection services, IConfiguration configuration)
    {
        var frontendOrigin = configuration[ApiConstants.FrontendOriginConfigurationKey];
        if (!Uri.TryCreate(frontendOrigin, UriKind.Absolute, out var origin)
            || (origin.Scheme != Uri.UriSchemeHttp && origin.Scheme != Uri.UriSchemeHttps)
            || origin.GetLeftPart(UriPartial.Authority) != frontendOrigin)
        {
            throw new InvalidOperationException(
                ApiConstants.InvalidFrontendOrigin);
        }

        services.AddCors(options => options.AddPolicy(ApiConstants.FrontendCorsPolicy, policy =>
            policy.WithOrigins(frontendOrigin).AllowAnyHeader().AllowAnyMethod().AllowCredentials()));
    }
}
