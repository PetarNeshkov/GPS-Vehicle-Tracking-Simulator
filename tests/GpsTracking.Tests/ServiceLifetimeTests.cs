using GpsTracking.Application.Abstractions;
using GpsTracking.Application.Simulation;
using GpsTracking.Application.Tracking.Commands;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace GpsTracking.Tests;

public sealed class ServiceLifetimeTests
{
    [Fact]
    public void RequestsHaveSeparateHandlersButShareTheirInMemoryStores()
    {
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.UseDefaultServiceProvider(options =>
            {
                options.ValidateScopes = true;
                options.ValidateOnBuild = true;
            }));
        using var firstRequest = factory.Services.CreateScope();
        using var secondRequest = factory.Services.CreateScope();
        var first = firstRequest.ServiceProvider;
        var second = secondRequest.ServiceProvider;

        Assert.Same(first.GetRequiredService<ITrackingStore>(), second.GetRequiredService<ITrackingStore>());
        Assert.Same(first.GetRequiredService<ISimulationSettingsStore>(), second.GetRequiredService<ISimulationSettingsStore>());
        Assert.Same(first.GetRequiredService<SubmitSampleHandler>(), first.GetRequiredService<SubmitSampleHandler>());
        Assert.NotSame(first.GetRequiredService<SubmitSampleHandler>(), second.GetRequiredService<SubmitSampleHandler>());
        Assert.NotSame(first.GetRequiredService<UpdateSimulationSettingsHandler>(), second.GetRequiredService<UpdateSimulationSettingsHandler>());
    }
}
