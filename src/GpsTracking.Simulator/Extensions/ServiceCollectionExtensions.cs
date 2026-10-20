using GpsTracking.Simulator.Configuration;
using GpsTracking.Simulator.Delivery;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GpsTracking.Simulator.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddServiceCollection(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IValidateOptions<SimulatorOptions>, SimulatorOptionsValidator>();
        services.AddOptions<SimulatorOptions>()
            .Bind(configuration.GetRequiredSection(SimulatorOptions.SectionName))
            .ValidateOnStart();

        services.AddSingleton(TimeProvider.System);
        services.AddHttpClient<TelemetryClient>((provider, client) =>
        {
            var options = provider.GetRequiredService<IOptions<SimulatorOptions>>().Value;
            client.BaseAddress = new Uri(options.BackendBaseUrl.TrimEnd('/') + '/');
            client.Timeout = TimeSpan.FromSeconds(SimulatorConstants.DeliveryTimeoutSeconds);
        });
        services.AddHostedService<SimulationWorker>();

        return services;
    }
}
