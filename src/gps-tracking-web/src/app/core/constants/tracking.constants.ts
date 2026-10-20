export const TRACKING = {
  vehicleHistoryPath: (id: string) => `/api/vehicles/${encodeURIComponent(id)}/history`,
  vehiclesPath: '/api/vehicles',
  settingsPath: '/api/settings',
  vehicleSettingsPath: (id: string) => `/api/vehicles/${encodeURIComponent(id)}/settings`,
  requestTimeoutMs: 5_000,
  colors: ['#0b7285', '#a84212'],
  view: {
    width: 1_000,
    height: 400,
    minLatitude: 42.6935,
    maxLatitude: 42.7015,
    minLongitude: 23.3100,
    maxLongitude: 23.3350
  }
} as const;
