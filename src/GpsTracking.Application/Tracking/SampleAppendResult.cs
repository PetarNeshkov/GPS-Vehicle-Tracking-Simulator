namespace GpsTracking.Application.Tracking;

public sealed record SampleAppendResult(SampleAcceptance Acceptance, VehicleSnapshot? Snapshot = null);
