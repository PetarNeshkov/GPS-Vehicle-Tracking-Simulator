using GpsTracking.Api.Constants;
using GpsTracking.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddServiceCollection(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors(ApiConstants.FrontendCorsPolicy);
app.MapControllers();

app.Run();

// Expose the API entry point to the in-process integration test host.
public partial class Program;
