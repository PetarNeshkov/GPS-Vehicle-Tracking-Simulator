export const REALTIME = {
  hubPath: '/hubs/tracking',
  vehicleUpdatedEvent: 'VehicleUpdated',
  globalSettingsUpdatedEvent: 'GlobalSettingsUpdated',
  retryDelayMs: 2_000,
  reconnectDelaysMs: [0, 2_000, 5_000, 10_000],
  status: {
    stopped: 'Live tracking stopped',
    connecting: 'Connecting to live tracking…',
    reconnecting: 'Reconnecting to live tracking…',
    synchronizing: 'Synchronizing vehicle positions…',
    connected: 'Live tracking connected'
  }
} as const;
