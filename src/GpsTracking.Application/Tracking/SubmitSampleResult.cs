namespace GpsTracking.Application.Tracking;

public sealed record SubmitSampleResult(SampleAcceptance Acceptance, string? ValidationError = null);
