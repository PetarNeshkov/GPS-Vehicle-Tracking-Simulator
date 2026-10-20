export const FRESHNESS = {
  staleAfterSeconds: 30,
  clockIntervalMs: 1_000,
  millisecondsPerSecond: 1_000,
  labels: {
    waiting: 'Waiting for GPS data',
    live: 'Live',
    stale: 'Stale'
  }
} as const;
