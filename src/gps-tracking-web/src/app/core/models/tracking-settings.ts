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
  readonly sessionId: string;
  readonly settings: GlobalSettings;
}

export interface TrackingAlert {
  readonly id: string;
  readonly vehicleId: string;
  readonly type: string;
  readonly occurredAt: string;
  readonly createdAt: string;
  readonly speedKmh: number;
  readonly speedLimitKmh: number;
  readonly speedingDurationSeconds: number;
}
