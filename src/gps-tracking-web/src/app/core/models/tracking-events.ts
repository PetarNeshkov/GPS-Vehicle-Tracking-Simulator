import { VehicleSnapshot } from './vehicle-snapshot';

export interface TrackingSnapshot {
  readonly sessionId: string;
  readonly vehicles: readonly VehicleSnapshot[];
}

export interface VehicleUpdatedEvent {
  readonly sessionId: string;
  readonly vehicle: VehicleSnapshot;
}

export type TrackingConnectionState = 'stopped' | 'connecting' | 'reconnecting' | 'connected';
