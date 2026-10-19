using System.Net;
using System.Net.Http.Json;
using GpsTracking.Application.Abstractions;
using GpsTracking.Application.Tracking;
using GpsTracking.Contracts;
using GpsTracking.Domain.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace GpsTracking.Tests;

public sealed class ExceptionHandlingTests
{
    private const string InternalFailureMessage = "Internal storage failure: private diagnostic details.";
    private const string ProblemJsonMediaType = "application/problem+json";

    public static TheoryData<string> HostEnvironments =>
        new() { Environments.Development, Environments.Production };

    [Theory]
    [MemberData(nameof(HostEnvironments))]
    public async Task UnexpectedFailureReturnsProblemDetailsWithoutExceptionInformation(string environment)
    {
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(environment);
            builder.ConfigureTestServices(services =>
                services.Replace(ServiceDescriptor.Singleton<ITrackingStore>(new FailingTrackingStore())));
        });
        using var client = factory.CreateClient();
        using var response = await client.GetAsync(ApiRoutes.VehiclesPath);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal(ProblemJsonMediaType, response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal((int)HttpStatusCode.InternalServerError, problem.Status);
        Assert.False(string.IsNullOrWhiteSpace(problem.Title));
        Assert.Null(problem.Detail);

        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain(InternalFailureMessage, body);
        Assert.DoesNotContain(nameof(InvalidOperationException), body);
        Assert.DoesNotContain(nameof(FailingTrackingStore), body);
    }

    private sealed class FailingTrackingStore : ITrackingStore
    {
        public VehicleSnapshot? UpdateSettings(string vehicleId, VehicleSettings settings, DateTimeOffset changedAt)
            => throw new InvalidOperationException(InternalFailureMessage);

        public GlobalSettingsSnapshot GetGlobalSettings()
            => throw new InvalidOperationException(InternalFailureMessage);

        public GlobalSettingsSnapshot UpdateGlobalSettings(GlobalSettings settings)
            => throw new InvalidOperationException(InternalFailureMessage);

        public SampleAppendResult AppendWithSnapshot(GpsSample sample, DateTimeOffset receivedAt)
            => throw new InvalidOperationException(InternalFailureMessage);

        public SampleAcceptance Append(GpsSample sample, DateTimeOffset receivedAt)
            => throw new InvalidOperationException(InternalFailureMessage);

        public IReadOnlyList<VehicleSnapshot> GetSnapshots()
            => throw new InvalidOperationException(InternalFailureMessage);
    }
}
