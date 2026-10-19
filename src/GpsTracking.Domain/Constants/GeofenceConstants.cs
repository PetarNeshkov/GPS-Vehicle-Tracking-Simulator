namespace GpsTracking.Domain.Constants;

public static class GeofenceConstants
{
    public const int ObservationWindowSize = 3;
    public const int RequiredMajority = 2;
    public const string EnteredAlertType = "GeofenceEntered";
    public const string ExitedAlertType = "GeofenceExited";
}
