using GpsTracking.Simulator.Extensions;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory
});

builder.Services.AddServiceCollection(builder.Configuration);

using var host = builder.Build();

await host.RunAsync();
