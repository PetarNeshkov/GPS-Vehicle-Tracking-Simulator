import { VehicleSnapshot } from './vehicle-snapshot';
import { GlobalSettings } from './tracking-settings';

export interface TrackingSnapshot {
  readonly globalSettings?: GlobalSettings;
  readonly sessionId: string;
  readonly vehicles: readonly VehicleSnapshot[];
}

export interface VehicleUpdatedEvent {
  readonly sessionId: string;
  readonly vehicle: VehicleSnapshot;
}

export type TrackingConnectionState = 'stopped' | 'connecting' | 'reconnecting' | 'connected';
