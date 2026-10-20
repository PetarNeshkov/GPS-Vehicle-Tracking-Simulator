import { MOVEMENT } from './movement.constants';
export const SETTINGS = {
  maximumDurationSeconds: 2_147_483_647,
  validation: {
    positive: 'Enter a finite number greater than zero.',
    duration: 'Enter a positive whole number of seconds, up to 2147483647.'
  },
  messages: {
    saveFailed: 'Settings could not be saved. Check the values and connection, then try again.',
    saved: 'Settings saved.',
    stopExplanation: MOVEMENT.stopsEnabled
  }
} as const;
