export const SETTINGS = {
  maximumDurationSeconds: 2_147_483_647,
  validation: {
    positive: 'Enter a finite number greater than zero.',
    duration: 'Enter a positive whole number of seconds, up to 2147483647.'
  },
  messages: {
    saveFailed: 'Settings could not be saved. Check the values and connection, then try again.',
    saved: 'Settings saved.',
    futureStops: 'Stop duration and radius are saved now; stop detection will be available in M5.'
  }
} as const;
