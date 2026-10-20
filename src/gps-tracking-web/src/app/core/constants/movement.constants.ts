export const MOVEMENT = {
  minimumLinePoints: 2,
  states: { Unknown: 'Awaiting movement evidence', Moving: 'Moving', PossibleStop: 'Possible stop', Stopped: 'Stopped' },
  historyFailed: 'GPS history could not be loaded. Try again.',
  historySessionChanged: 'The backend session changed. Wait for synchronization and load history again.',
  stopsEnabled: 'Stops use the global radius and each vehicle’s stop duration. Completed routes keep their original boundaries.'
} as const;
