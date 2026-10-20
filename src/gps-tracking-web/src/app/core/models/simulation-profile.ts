export interface SimulationProfile {
  readonly cruisingSpeedKmh: number;
  readonly speedingSpeedKmh: number;
  readonly drivingSeconds: number;
  readonly speedingSeconds: number;
  readonly recoverySeconds: number;
  readonly stopSeconds: number;
  readonly resumeSeconds: number;
}
