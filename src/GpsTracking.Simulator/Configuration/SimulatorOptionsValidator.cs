using Microsoft.Extensions.Options;

namespace GpsTracking.Simulator.Configuration;

public sealed class SimulatorOptionsValidator : IValidateOptions<SimulatorOptions>
{
    public ValidateOptionsResult Validate(string? name, SimulatorOptions options)
    {
        var errors = new List<string>();
        if (!Uri.TryCreate(options.BackendBaseUrl, UriKind.Absolute, out var backend)
            || (backend.Scheme != Uri.UriSchemeHttp && backend.Scheme != Uri.UriSchemeHttps))
            errors.Add("Simulator:BackendBaseUrl must be an absolute HTTP(S) URL.");

        if (options.TickIntervalSeconds < 10)
            errors.Add("Simulator:TickIntervalSeconds must be at least 10.");

        var vehicleIds = options.VehicleIds ?? [];
        if (vehicleIds.Length != 2 || vehicleIds.Any(string.IsNullOrWhiteSpace)
            || vehicleIds.Distinct(StringComparer.Ordinal).Count() != 2)
            errors.Add("Simulator:VehicleIds must contain exactly two distinct non-empty identifiers.");

        return errors.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(errors);
    }
}
