import { Movement, RouteSegment } from './movement';
import { GeofenceMembership } from './geofence';
import { TrackingAlert, VehicleSettings } from './tracking-settings';

export interface GpsSample {
  readonly timestamp: string;
  readonly latitude: number;
  readonly longitude: number;
  readonly speedKmh: number;
  readonly heading: number;
}

export interface VehicleSnapshot {
  readonly movement?: Movement;
  readonly segments?: readonly RouteSegment[];
  readonly geofenceMemberships?: readonly GeofenceMembership[];
  readonly settings?: VehicleSettings;
  readonly alerts?: readonly TrackingAlert[];
  readonly id: string;
  readonly name: string;
  readonly latestSample: GpsSample | null;
  readonly lastReceivedAt: string | null;
  readonly sampleCount: number;
  readonly revision: number;
}
