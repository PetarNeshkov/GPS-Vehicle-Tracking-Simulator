export const ALERTS = {
  pageSize: 20,
  labels: {
    Speeding: 'Speeding',
    GeofenceEntered: 'Geofence entered',
    GeofenceExited: 'Geofence exited'
  } as Readonly<Record<string, string>>
} as const;
