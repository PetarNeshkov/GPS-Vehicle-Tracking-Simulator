namespace GpsTracking.Simulator.Movement;

public sealed record RoutePoint(double Latitude, double Longitude);

public sealed record RoutePosition(double Latitude, double Longitude, double SpeedKmh, double Heading);
