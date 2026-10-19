using GpsTracking.Api.Constants;
using GpsTracking.Api.Extensions;
using GpsTracking.Api.Realtime;
using GpsTracking.Contracts.Tracking;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddServiceCollection(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseExceptionHandler();
app.UseCors(ApiConstants.FrontendCorsPolicy);
app.MapControllers();
app.MapHub<TrackingHub>(TrackingHubContract.Path);

app.Run();

// Expose the API entry point to the in-process integration test host.
public partial class Program;
