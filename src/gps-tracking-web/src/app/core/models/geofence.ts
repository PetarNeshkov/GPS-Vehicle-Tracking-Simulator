export interface Geofence {
  readonly id: string;
  readonly name: string;
  readonly minLatitude: number;
  readonly maxLatitude: number;
  readonly minLongitude: number;
  readonly maxLongitude: number;
}

export type GeofenceState = 'Unknown' | 'Inside' | 'Outside';

export interface GeofenceMembership {
  readonly geofenceId: string;
  readonly state: GeofenceState;
}
