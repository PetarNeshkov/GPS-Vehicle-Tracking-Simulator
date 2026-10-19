export const ERROR_MESSAGES = {
  trackingConnectionFailed:
    'Cannot reach the tracking service. Showing the last received data; retrying automatically.',
  trackingSynchronizationFailed:
    'Cannot synchronize vehicle positions. Showing the last received data; retrying automatically.'
} as const;
