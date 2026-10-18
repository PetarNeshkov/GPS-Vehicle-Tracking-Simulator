export interface GpsSample {
  readonly timestamp: string;
  readonly latitude: number;
  readonly longitude: number;
  readonly speedKmh: number;
  readonly heading: number;
}

export interface VehicleSnapshot {
  readonly id: string;
  readonly name: string;
  readonly latestSample: GpsSample | null;
  readonly lastReceivedAt: string | null;
  readonly sampleCount: number;
  readonly revision: number;
}
