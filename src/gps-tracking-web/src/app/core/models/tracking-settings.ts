import { VehicleSnapshot } from './vehicle-snapshot';
export interface VehicleSettings {
  readonly speedLimitKmh: number;
  readonly speedingDurationSeconds: number;
  readonly stopDurationSeconds: number;
}

export interface GlobalSettings {
  readonly stopRadiusMeters: number;
  readonly revision: number;
}

export interface GlobalSettingsEvent {
  readonly vehicles?: readonly VehicleSnapshot[];
  readonly sessionId: string;
  readonly settings: GlobalSettings;
}

export interface TrackingAlert {
  readonly geofenceId?: string | null;
  readonly geofenceName?: string | null;
  readonly id: string;
  readonly vehicleId: string;
  readonly type: string;
  readonly occurredAt: string;
  readonly createdAt: string;
  readonly speedKmh: number | null;
  readonly speedLimitKmh: number | null;
  readonly speedingDurationSeconds: number | null;
}
