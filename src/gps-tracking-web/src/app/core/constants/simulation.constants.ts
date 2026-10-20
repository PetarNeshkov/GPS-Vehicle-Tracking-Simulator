export const SIMULATION = {
  path: (id: string) => `/api/vehicles/${encodeURIComponent(id)}/simulation`,
  messages: {
    loadFailed: 'Simulation settings could not be loaded. Try again.',
    saveFailed: 'Simulation settings could not be saved. Your edits have been kept.',
    invalid: 'Use positive speeds and positive whole seconds for every duration.',
    saved: 'Saved. The simulator applies changes on its next reporting cycle when running.'
  },
  fields: [
    { key: 'cruisingSpeedKmh', label: 'Cruising speed (km/h)', duration: false },
    { key: 'speedingSpeedKmh', label: 'Speeding phase speed (km/h)', duration: false },
    { key: 'drivingSeconds', label: 'Initial driving (seconds)', duration: true },
    { key: 'speedingSeconds', label: 'Speeding phase (seconds)', duration: true },
    { key: 'recoverySeconds', label: 'Normal driving after speeding (seconds)', duration: true },
    { key: 'stopSeconds', label: 'Stationary phase (seconds)', duration: true },
    { key: 'resumeSeconds', label: 'Resumed driving (seconds)', duration: true }
  ]
} as const;
