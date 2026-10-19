export const GEOFENCE = {
  states: {
    Unknown: 'Determining membership',
    Inside: 'Inside',
    Outside: 'Outside'
  },
  alertTypes: {
    speeding: 'Speeding',
    entered: 'GeofenceEntered',
    exited: 'GeofenceExited'
  },
  messages: {
    confirmation: 'Membership uses two of the latest three GPS observations; it may lag behind the marker.',
    entered: 'Entered',
    exited: 'Exited'
  }
} as const;
