using GpsTracking.Application.Simulation;
using GpsTracking.Contracts.Tracking;
using Microsoft.AspNetCore.Mvc;

namespace GpsTracking.Api.Extensions;

public static class SimulationMappingExtensions
{
    public static SimulationSettings ToSettings(this SimulationProfile profile) => new(
        profile.CruisingSpeedKmh, profile.SpeedingSpeedKmh, profile.DrivingSeconds,
        profile.SpeedingSeconds, profile.RecoverySeconds, profile.StopSeconds, profile.ResumeSeconds);

    public static SimulationProfile ToResponse(this SimulationSettings settings) => new(
        settings.CruisingSpeedKmh, settings.SpeedingSpeedKmh, settings.DrivingSeconds,
        settings.SpeedingSeconds, settings.RecoverySeconds, settings.StopSeconds, settings.ResumeSeconds);

    public static IActionResult ToSimulationResult(this SimulationSettings? settings, ControllerBase controller)
        => settings is null ? controller.NotFound() : settings.ToResponse().ToOkResult();

    public static IActionResult ToSimulationResult(this SimulationUpdateStatus status,
        SimulationProfile profile, ControllerBase controller) => status switch
    {
        SimulationUpdateStatus.Updated => profile.ToOkResult(),
        SimulationUpdateStatus.NotFound => controller.NotFound(),
        _ => controller.Problem(statusCode: StatusCodes.Status400BadRequest, detail: SimulationSettings.InvalidSettings)
    };
}
